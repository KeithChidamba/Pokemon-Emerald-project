using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class Turn_Based_Combat : MonoBehaviour,IInjectable
{
    [SerializeField]List<Turn> _turnHistory = new();
    public event Action OnNewTurn;
    public event Func<Battle_Participant,IEnumerator> OnMoveExecute;
    public event Action OnTurnsCompleted;
    public int currentTurnIndex;
    public bool faintEventDelay;
    public WeatherCondition currentWeather;
    public WeatherCondition clearWeather;
    private event Func<IEnumerator> OnWeatherEffect;
    public event Action OnWeatherEnd;

    private event Action<bool> OnAttackAttempted;

    private Dialogue_handler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private BattleIntro _battleIntroHandler;
    private Pokemon_party _pokemonPartyHandler;
    private BattleVisuals _battleVisualsHandler;
    private Battle_handler _battleHandler;
    private Move_handler _moveUsageHandler;
    private MoveLogicHandler _moveLogicHandler;
    private BattleOperations _battleOperationsHandler;
    private Game_Load _gameLoadingHandler;
    private Options_manager _dialogueOptionsHandler;
    private Game_ui_manager _gameUIManager;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueOptionsHandler = container.Resolve<Options_manager>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        _battleOperationsHandler = container.Resolve<BattleOperations>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _battleIntroHandler = container.Resolve<BattleIntro>();
        _battleHandler = container.Resolve<Battle_handler>();
        _moveUsageHandler = container.Resolve<Move_handler>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _moveLogicHandler = container.Resolve<MoveLogicHandler>();
        _gameUIManager = container.Resolve<Game_ui_manager>();
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
    {
        _battleHandler.OnBattleEnd += ResetTurnState;
        OnNewTurn += ()=> StartCoroutine(CheckParticipantCoolDown());
        _battleHandler.OnSwitchOut += RemoveWeatherBuffReceiver;
        clearWeather = new WeatherCondition(Weather.Clear);
        currentWeather = clearWeather;
    }
    
    public void SaveTurn(Turn turn)
    {
        Debug.Log("saved");
        _turnHistory.Add(turn);
        if ((_battleHandler.isDoubleBattle && IsLastParticipant())
            || (currentTurnIndex == _battleHandler.participantCount))
        {
            _inputStateHandler.AddPlaceHolderState();
            SetPriority();
            StartCoroutine(ExecuteMoves());    
        }
        else
        {
            _inputStateHandler.ResetRelevantUi(new[]{InputStateName.PokemonBattleMoveSelection
                ,InputStateName.PokemonBattleEnemySelection});
            NextTurn();
        }
    }

    public void SaveSwitchTurn(SwitchOutData data)
    {
        var fakeMove = ScriptableObject.CreateInstance<Move>();
        fakeMove.priority = 0;
            
        var switchTurn = new Turn(TurnUsage.SwitchOut,
            attacker: currentTurnIndex,move:fakeMove);
            
        switchTurn.switchData = data;
        SaveTurn(switchTurn);
    }
    private bool IsLastParticipant()
    {
        var livingParticipants = _battleHandler.battleParticipants.ToList();
        livingParticipants.RemoveAll(participant => participant.pokemon==null);
        if (livingParticipants.Last() ==
            _battleHandler.battleParticipants[currentTurnIndex])
            return true;
        return false;
    }
    private void ResetTurnState()
    {
        currentTurnIndex = 0;
        currentWeather.weather = Weather.Clear;
        _turnHistory.Clear();
        faintEventDelay = false;
        StopAllCoroutines();
    }

    private void ModifyMoveAccuracy(Turn turn)
    {
        if (turn.move.moveName == NameDB.GetMoveName(LearnSetMoveName.Thunder))
        {
            if (currentWeather.weather == Weather.Rain)
                turn.move.isSureHit = true;
            if (currentWeather.weather == Weather.Sunlight)
                turn.move.moveAccuracy = 50f;
        }
        //add more when more moves need it
    }
    private IEnumerator CheckAttackSuccess(Turn turn, Battle_Participant attacker,Battle_Participant victim)
    {
        if(attacker.pokemon.hp<=0)
        {
            OnAttackAttempted?.Invoke(false);
            yield break;
        }
        if (attacker.canAttack)
        {
            if (attacker.isConfused)
            {
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonName + " is confused");
                yield return _battleVisualsHandler.DisplayConfusionVisuals(attacker);
                if (Utility.RandomRange(0, 2) < 1)
                {
                    _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonName+" hurt itself in its confusion");
                    yield return _moveUsageHandler.DealConfusionDamage(attacker);
                    OnAttackAttempted?.Invoke(false);
                    yield break;
                }
            }
            if (attacker.isInfatuated)
            {
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonName + " is in love ");
                if (Utility.RandomRange(0, 2) < 1)
                {
                    _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonName+" can’t move because of love");
                    OnAttackAttempted?.Invoke(false);
                    yield break;
                }
            }
            
            if(!attacker.semiInvulnerabilityData.executionTurn && !attacker.currentCoolDown.isCoolingDown)
            {
                _dialogueHandler.DisplayBattleInfo(GetMoveUsageText(turn.move,attacker, victim));
            }

            ModifyMoveAccuracy(turn);
            
            if (victim.isSemiInvulnerable)
            {
                if (turn.move.isSelfTargeted)
                {
                    OnAttackAttempted?.Invoke(true);
                    yield break;
                }
                if (victim.semiInvulnerabilityData.IsInvulnerableTo(turn.move))
                {
                    _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonName +
                                                                victim.semiInvulnerabilityData.displayMessage);
                    OnAttackAttempted?.Invoke(false);
                    yield break;
                }
            }

            if (!turn.move.isSureHit)
            {
                if (!MoveSuccessful(turn))
                {

                    if (attacker.pokemon.accuracy >= victim.pokemon.evasion)
                        _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonName +
                                                                    " missed the attack");
                    else
                        _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonName +
                                                                    " dodged the attack");
                }
                else
                {
                    OnAttackAttempted?.Invoke(true);
                    yield break;
                }
            }
            else
            {
                OnAttackAttempted?.Invoke(true);
                yield break;
            }
            
        }
        else
        {
            if (attacker.isFlinched)
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonName+" flinched!");
            else if (attacker.pokemon.statusEffect != StatusEffect.None)
            {
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonName+" is affected by "+ attacker.pokemon.statusEffect);
                yield return _battleVisualsHandler.DisplayStatusEffectVisuals(attacker);
            }
        }
        OnAttackAttempted?.Invoke(false);
    }
    
    private IEnumerator ExecuteMoves()
    {            
        bool successfulAttack = false;
        void GetAttackResult(bool result)
        {
             OnAttackAttempted -= GetAttackResult;
             successfulAttack = result;
        }
        
//handle all swaps
        var switchTurns = new List<int>();
        
        for(var i = 0;i < _turnHistory.Count;i++)
        { 
            if (_turnHistory[i].turnUsage == TurnUsage.SwitchOut)
            {
                switchTurns.Add(i);
                yield return HandleSwap(_turnHistory[i].switchData);
                yield return new WaitUntil(()=>!_dialogueHandler.messagesLoading);
            }
        }

        if (switchTurns.Count > 0)
        {
            var orderTurns = switchTurns.OrderByDescending(itemIndex=>itemIndex).ToList();//prevent index out of range when removing turns
            orderTurns.ForEach(index => _turnHistory.RemoveAt(index));
            Debug.Log("turn ortd: " + orderTurns.Count);
        }
        
//handle all attacks
        foreach (var currentTurn in _turnHistory )
        {
            if (_battleHandler.battleOver) break;

            if (currentTurn.isCancelled) continue;
            
            currentTurn.turnExecuted = true;
            
            var attacker=_battleHandler.battleParticipants[currentTurn.attackerIndex];
            var victim=_battleHandler.battleParticipants[currentTurn.victimIndex];
            
            attacker.isSemiInvulnerable = false;
            
            //check on participants
            if (!IsValidParticipantState(attacker))
                continue;
            
            if (!IsValidParticipant(currentTurn,attacker))
                continue;
            
            if (!IsValidParticipantState(victim))
            {
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonName+" missed the attack");
                yield return new WaitUntil(()=>!_dialogueHandler.messagesLoading);
                continue;
            }
           
            yield return OnMoveExecute?.Invoke(attacker);
           
            yield return attacker.heldItemHandler.CheckForUsableItem();

            yield return new WaitUntil(()=>!_dialogueHandler.messagesLoading);
            
            successfulAttack = false;
            OnAttackAttempted += GetAttackResult;
            
            yield return CheckAttackSuccess(currentTurn,attacker,victim);
            yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
            
            yield return new WaitUntil(() => _battleHandler.faintQueue.Count == 0 && !faintEventDelay);
            CheckRepeatedMove(attacker,currentTurn.move);
            if (successfulAttack)
            {
                _moveUsageHandler.doingMove = true;
                _moveUsageHandler.ExecuteMove(currentTurn);
                
                yield return new WaitUntil(() => !_moveUsageHandler.doingMove);
                yield return new WaitUntil(() => _battleHandler.faintQueue.Count == 0 && !faintEventDelay);
            }
            else
            {
                attacker.previousMove.failedAttempt = true;
                attacker.semiInvulnerabilityData.ResetState();
            }
        }
        yield return new WaitUntil(() => _battleHandler.faintQueue.Count == 0 && !faintEventDelay);
        yield return new WaitUntil(()=> !_dialogueHandler.messagesLoading);
        
        _turnHistory.Clear();
        OnTurnsCompleted?.Invoke();
        
        var validList = _battleHandler.GetValidParticipants();
        
        foreach (var participant in validList)
        {
            yield return participant.statusHandler.CheckStatus();
        }
        yield return new WaitUntil(() => _battleHandler.faintQueue.Count == 0 && !faintEventDelay);
        
        //damage from weather
        if (currentWeather.weather != Weather.Clear)
        {
            ReduceWeatherDuration();
            yield return ExecuteWeatherEffect();
        }
        yield return new WaitUntil(() => _battleHandler.faintQueue.Count == 0 && !faintEventDelay);
        
        yield return new WaitUntil(()=> !_dialogueHandler.messagesLoading);
        
        //semi-invulnerability turn logic and cooldown check
        foreach(var participant in validList)
        {
            if (participant.currentCoolDown.isCoolingDown)
            {
                if (participant.currentCoolDown.numTurns == 0)
                {
                    if (participant.currentCoolDown.turnData.isCancelled)
                    {
                        participant.currentCoolDown.ResetState();
                    }
                    else
                    {
                        participant.currentCoolDown.executeTurn = true;
                        _turnHistory.Add(new(participant.currentCoolDown.turnData));
                        NextTurn();
                    }
                }
            }
            
            participant.pokemon.ResetMoveData();
            
            if(!participant.isSemiInvulnerable)continue;
            
            _turnHistory.Add(new Turn(participant.semiInvulnerabilityData.turnData));
        }
        
        yield return new WaitUntil(()=> !_dialogueHandler.messagesLoading);
        NextTurn();
    }
    public IEnumerator AllowPlayerSwitchIn(string trainerName,string pokemonName)
    {
        if (_battleHandler.currentBattleStyle != Battle_handler.BattlesStyle.Switch) yield break;
        
        _dialogueHandler.DisplayList($"{trainerName} is about to use {pokemonName}",
            new[] { InteractionOptions.None, InteractionOptions.None},
            new[] { "Yes", "No" });
 
        _dialogueOptionsHandler.OnInteractionOptionChosen += AwaitPartyOpen;
        bool processing = true;
        yield return new WaitUntil(() => !processing);
        
        //carry on with battle
        
        void AwaitPartyOpen(Interaction interaction,int optionChosen)
        {
            if (optionChosen > 0)
            {
                processing = false;
                return;
            }
            processing = true;
            _pokemonPartyHandler.swapOutNext = true;
            _pokemonPartyHandler.OnMemberSelected += ResetEvent;
            _gameUIManager.ViewPokemonParty();
        }
        void ResetEvent(int memberPosition)
        {
            _pokemonPartyHandler.OnMemberSelected -= ResetEvent;
            processing = false;
        }
        yield return null;
    }
    public IEnumerator HandleSwap(SwitchOutData swap, bool forcedSwap=false)
    {
        if (forcedSwap)
        {
            _dialogueHandler.DisplayBattleInfo(swap.Participant.pokemon.pokemonName
                                                        + " was blown out");
        }
        else
        {
            if (swap.Participant.isPlayer)
            {
                _dialogueHandler.DisplayBattleInfo(_gameLoadingHandler.playerData.playerName
                                                            + " withdrew " + swap.Participant.pokemon.pokemonName);
            }
            else
            {
                _dialogueHandler.DisplayBattleInfo(swap.Participant.pokemonTrainerAI.trainerData.TrainerName
                                                            +" withdrew "+swap.Participant.pokemon.pokemonName);
            }
        }
        
        if (swap.Participant.isPlayer || forcedSwap)
        {
            yield return _battleVisualsHandler.WithdrawPokemon(swap.Participant);
        }
        //check if move used was pursuit
        var pursuitUsersTurn = _turnHistory.FirstOrDefault(turn => 
            turn.move.moveName == NameDB.GetMoveName(LearnSetMoveName.Pursuit));
        
        if(pursuitUsersTurn is { turnExecuted: false })
        {
            var attacker=_battleHandler.battleParticipants[pursuitUsersTurn.attackerIndex];
            var victim=_battleHandler.battleParticipants[pursuitUsersTurn.victimIndex];
            
            if (victim == swap.Participant)
            {
                bool pokemonFainted = false;

                pursuitUsersTurn.isCancelled = true;//since it strikes here
                
                void CancelOnFaint()
                {
                    victim.OnPokemonFainted -= CancelOnFaint;
                    pokemonFainted = true;
                }

                victim.OnPokemonFainted += CancelOnFaint;

                yield return _moveLogicHandler.Pursuit(attacker, victim, pursuitUsersTurn.move);
                if (pokemonFainted) yield break;
            }
        }
        
        if (swap.Participant.isPlayer)
        {
            swap.Participant.ResetParticipantState();
            var party = _pokemonPartyHandler.party;
            (party[swap.PartyPosition], party[swap.MemberToSwapWith]) = (party[swap.MemberToSwapWith], party[swap.PartyPosition]);
            _pokemonPartyHandler.UpdateUIAfterSwap();
            
            _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
            yield return _battleIntroHandler.SwitchInPokemon(swap.Participant,party[swap.PartyPosition]);
        }
        else
        {
            swap.Participant.ResetParticipantState();
            var enemyParty = swap.Participant.pokemonTrainerAI.trainerParty;
            (enemyParty[swap.PartyPosition], enemyParty[swap.MemberToSwapWith]) = (enemyParty[swap.MemberToSwapWith], enemyParty[swap.PartyPosition]);
            yield return _battleIntroHandler.SwitchInPokemon(swap.Participant,enemyParty[swap.PartyPosition]);
        }
    }

    public bool ContainsSwitch(int memberPosition)
    {
        foreach (var turn in _turnHistory)
        {
            if (turn.turnUsage==TurnUsage.SwitchOut)//must check this first to avoid null ref
            {
                if (turn.switchData.MemberToSwapWith==memberPosition)
                {
                    return true;
                }
            }
        }
        return false;
    }
    public string GetMoveUsageText(Move move, Battle_Participant attacker,Battle_Participant victim)
    {
        if (move.displayTargetMessage)
            return attacker.pokemon.pokemonName + " used " 
                                                + move.moveName + " on " + victim.pokemon.pokemonName + "!";
        return attacker.pokemon.pokemonName + " used " + move.moveName + "!";
    }

    private IEnumerator CheckParticipantCoolDown()
    {
        if (_battleHandler.battleOver) yield break;
        var participant = _battleHandler.GetCurrentParticipant();
        if (!participant.currentCoolDown.isCoolingDown) yield break;
        if (participant.currentCoolDown.executeTurn) yield break;
        
        if (participant.currentCoolDown.displayMessage)
        {
            _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonName
                                                        +participant.currentCoolDown.message);
            yield return new WaitUntil(()=>!_dialogueHandler.messagesLoading);
        }
        participant.currentCoolDown.numTurns--;
        NextTurn();
        
    }
    private void CheckRepeatedMove(Battle_Participant attacker, Move move)
    {
        if (attacker.previousMove==null)
        {
            var newData = new PreviousMove(move,0);
            attacker.previousMove = newData;
            return;
        }

        if (attacker.previousMove.move.moveName == move.moveName)
        {
            attacker.previousMove.numRepetitions++;
            attacker.previousMove.failedAttempt = false;
        }
        else
        {
            var newData = new PreviousMove(move,0);
            attacker.previousMove = newData;
        }
    }
    private bool IsValidParticipantState(Battle_Participant participant)
    {
        if (!participant.isActive) return false;
        return participant.pokemon is { hp: >= 0 };
    }

    private bool IsValidParticipant(Turn turn,Battle_Participant participant)
    {
        return turn.attackerID == participant.pokemon.pokemonID||
                turn.victimID == participant.pokemon.pokemonID;
    }
    public void NextTurn()
    {
        if (_battleHandler.isDoubleBattle)
            ChangeTurn(3, 1);
        else
            ChangeTurn(2, 2);

        if (_battleHandler.GetCurrentParticipant().isSemiInvulnerable)
            NextTurn();
    }

    public void RemoveTurn()
    {
        //player wants to change their turn usage
        _turnHistory.RemoveAt(currentTurnIndex-1);
        currentTurnIndex --;
        _inputStateHandler.OnStateRemoved += _battleHandler.SetupOptionsInput;
    }
    private void ChangeTurn(int maxParticipantIndex,int step)
    {
        if (currentTurnIndex < maxParticipantIndex)
            currentTurnIndex+=step;
        else
            currentTurnIndex = 0;
        
        if (!_battleHandler.battleParticipants[currentTurnIndex].isActive) NextTurn();
        
        OnNewTurn?.Invoke();
    }
    private bool MoveSuccessful(Turn turn)
    {
        var random = Utility.RandomRange(1, 100);
        var hitChance = turn.move.moveAccuracy *
                           (_battleHandler.battleParticipants[turn.attackerIndex].pokemon.accuracy / 
                            _battleHandler.battleParticipants[turn.victimIndex].pokemon.evasion);
        return hitChance>random;
    }
    private void SetPriority()
    {
        var orderBySpeed = _turnHistory.OrderByDescending(p => _battleHandler.battleParticipants[p.attackerIndex].pokemon.speed).ToList();
        var priorityList = orderBySpeed.OrderByDescending(p => p.move.priority).ToList();
        _turnHistory.Clear();
        _turnHistory.AddRange(priorityList);
    }

    public void ChangeWeather(WeatherCondition newWeather,bool fromAbility=false)
    {
        OnWeatherEffect -= currentWeather.weatherEffect;
        
        if (fromAbility)
            newWeather.isInfinite = true;
        else
            newWeather.turnDuration = 5;
        
        switch (newWeather.weather)
        {
            case Weather.Sandstorm:
                newWeather.weatherEffect = SandStormEffect;
                newWeather.weatherBegunMessage = "A sandstorm brewed!";
                newWeather.weatherTurnEndMessage = "The sandstorm rages.";
                newWeather.weatherDamageMessage = " is buffeted by the sandstorm!";
                newWeather.weatherEndMessage = "The sandstorm subsided.";
                break;
            case Weather.Rain:
                newWeather.weatherEffect = RainEffect;
                newWeather.weatherBegunMessage = "It started to rain!";
                newWeather.weatherTurnEndMessage = "Rain continues to fall.";
                newWeather.weatherEndMessage = "The rain stopped.";
                break;
            case Weather.Hail:
                newWeather.weatherEffect = HailEffect;
                newWeather.weatherBegunMessage = "It started to hail!";
                newWeather.weatherTurnEndMessage = "Hail continues to fall.";
                newWeather.weatherDamageMessage = " is pelted by hail!";
                newWeather.weatherEndMessage = "The hail stopped.";
                break;
            case Weather.Sunlight:
                newWeather.weatherEffect = SunEffect;
                newWeather.weatherBegunMessage = "The sunlight got bright!";
                newWeather.weatherTurnEndMessage = "The sunlight is strong.";
                newWeather.weatherEndMessage = "The sunlight faded.";
                break;
        }
        _dialogueHandler.DisplayBattleInfo(newWeather.weatherBegunMessage);
        OnWeatherEffect += newWeather.weatherEffect;
        currentWeather = newWeather;
    }

    private void ReduceWeatherDuration()
    {
        if (currentWeather.isInfinite) return;
        if (currentWeather.turnDuration == 0)
        {
            OnWeatherEnd?.Invoke();
            OnWeatherEffect -= currentWeather.weatherEffect;
            _dialogueHandler.DisplayBattleInfo(currentWeather.weatherEndMessage);
            currentWeather = clearWeather;
            return;
        }
        currentWeather.turnDuration--;
    }

    private IEnumerator ExecuteWeatherEffect()
    {
        _dialogueHandler.DisplayBattleInfo(currentWeather.weatherTurnEndMessage);
        yield return OnWeatherEffect?.Invoke();
    }

    private void RemoveWeatherBuffReceiver(Battle_Participant participant)
    {
        if (currentWeather.weather == Weather.Clear) return;
        if (!currentWeather.buffedParticipants.Contains(participant)) return;
        currentWeather.buffedParticipants.Remove(participant);
    }
    private IEnumerator SandStormEffect()
    {
        var protectedTypes = new[]{
            Types.Rock, Types.Ground, Types.Steel
        };
        var validParticipants = _battleHandler.GetValidParticipants();
        foreach (var participant in validParticipants)
        {
            var isProtected = false;
            foreach (var protectedType in protectedTypes)
            {
                if (participant.pokemon.HasType(protectedType))
                {
                    isProtected = true;
                    if(!currentWeather.buffedParticipants.Contains(participant))
                    {
                        if (protectedType == Types.Rock)
                        {
                            //buff rock types
                            var spDefBuff = new BuffDebuffData(participant,
                                Stat.SpecialDefense, true, 1);
                            _battleOperationsHandler.canDisplayChange = false;
                            _moveUsageHandler.ExecuteBuffOrDebuff(spDefBuff);
                            currentWeather.buffedParticipants.Add(participant);
                        }
                    }
                    break;
                }
            }
            if (isProtected) continue;
            yield return DealWeatherDamage(participant);
        }
    }
    private IEnumerator RainEffect()
    {
        DamageModifierForWeather( Types.Fire,0.5f);
        DamageModifierForWeather( Types.Water,1.5f);
        yield return null;
    }
    private IEnumerator SunEffect()
    {
        DamageModifierForWeather( Types.Fire,1.5f);
        DamageModifierForWeather( Types.Water,0.5f);
        yield return null;
    }
    private IEnumerator HailEffect()
    {
        var validParticipants = _battleHandler.GetValidParticipants();
        foreach (var participant in validParticipants)
        {
            if (participant.pokemon.HasType(Types.Ice)) continue;
            yield return DealWeatherDamage(participant);
        }
    }

    private void DamageModifierForWeather(Types type,float damageModifier)
    { 
        var damageModifierInfo = ScriptableObject.CreateInstance<DamageModifierInfo>();
        damageModifierInfo.typeAffected = type;
        damageModifierInfo.damageModifier = damageModifier;
        var modifier = new OnFieldDamageModifier(_battleHandler,_moveUsageHandler,this,damageModifierInfo,removeOnSwitch:false);
        OnWeatherEnd += modifier.RemoveAfterWeather;
        _moveUsageHandler.AddFieldDamageModifier(modifier);
    }
    private IEnumerator DealWeatherDamage(Battle_Participant victim)
    {
        _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonName + currentWeather.weatherDamageMessage);
        var weatherDamage = victim.pokemon.maxHp * (1 / 16f);
        _moveUsageHandler.DisplayDamage(victim,isSpecificDamage:true,
            predefinedDamage:weatherDamage,displayEffectiveness:false);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
    }
}
