using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class TurnBasedCombatHandler : MonoBehaviour,IInjectable
{
    [SerializeField]List<Turn> _turnHistory = new();
    public event Action OnNewTurn;
    public event Func<BattleParticipant,IEnumerator> OnMoveExecute;
    public event Action OnTurnsCompleted;
    public int CurrentTurnIndex => currentTurnIndex;
    [SerializeField]private int currentTurnIndex;
    
    public WeatherCondition CurrentWeather { get; private set; }
    
    [SerializeField]private WeatherCondition clearWeather;
    private event Func<IEnumerator> OnWeatherEffect;
    public event Action OnWeatherEnd;
    private event Action<bool> OnAttackAttempted;

    private DialogueHandler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private BattleIntro _battleIntroHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    private BattleVisuals _battleVisualsHandler;
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
    private MoveLogicHandler _moveLogicHandler;
    private GameLoadingHandler _gameLoadingHandler;
    
    public void Inject(ServiceContainer container)
    {
        _gameLoadingHandler = container.Resolve<GameLoadingHandler>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _battleIntroHandler = container.Resolve<BattleIntro>();
        _battleHandler = container.Resolve<BattleHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _moveLogicHandler = container.Resolve<MoveLogicHandler>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _battleHandler.OnBattleEnd += ResetTurnState;
        OnNewTurn += ()=> StartCoroutine(CheckParticipantCoolDown());
        _battleHandler.OnSwitchOut += RemoveWeatherBuffReceiver;
        clearWeather = new WeatherCondition(Weather.Clear);
        CurrentWeather = clearWeather;
    }
    private void AddTurn(Turn turn)
    {
        //Debug.Log($"turn added {turn.turnUsage}");
        _turnHistory.Add(turn);
    }
    private void RemoveTurn(int index)
    {
        //Debug.Log("turn removed");
        _turnHistory.RemoveAt(index);
    }
    private void ClearTurn()
    {
        //Debug.Log("turn cleared");
        _turnHistory.Clear();
    }

    private void BeginTurnExecution()
    {
        _inputStateHandler.AddPlaceHolderState();
        SetPriority();
        StartCoroutine(ExecuteMoves()); 
    }
    public void SaveTurn(Turn turn)
    {
        if (_turnHistory.Any(t => t.attackerID == turn.attackerID))
        {
            //for testing incase new bug
            Debug.Log("duplicate detected, index of : "+turn.attackerKey);
            return;
        }
        AddTurn(turn);
        _battleHandler.SetPlayerTurnUsage(PlayerTurnUsage.Fight);
        if ((_battleHandler.isDoubleBattle && IsLastParticipant())
            || currentTurnIndex == _battleHandler.validParticipantCount)
        {
            BeginTurnExecution();
        }
        else
        {
            _inputStateHandler.ResetRelevantUi(new[]{InputStateName.PokemonBattleMoveSelection
                ,InputStateName.PokemonBattleEnemySelection});
            NextTurn();
        }
    }
    public void SaveStruggleTurn(BattleParticipant attacker)
    {
        var struggle = ScriptableObject.CreateInstance<Move>();
        struggle.priority = 0;
        struggle.moveName = "Struggle";
        struggle.isSpecial = false;
        var typelessType = ScriptableObject.CreateInstance<Type>();
        typelessType.typeEnum = PokemonType.Typeless;
        struggle.type = typelessType;
        
        var validEnemyKeys = attacker.currentEnemies
            .Where(enemy => enemy.isActive)
            .Select(enemy => enemy.participantKey)
            .ToList();
        
        var randomEnemyKey = validEnemyKeys[Utility.RandomRange(0, validEnemyKeys.Count)];
        var victim = _battleHandler.GetParticipant(randomEnemyKey);
        
        var struggleTurn = new Turn(
            TurnUsage.UseStruggle,
            attackerKey : _battleHandler.GetCurrentParticipant().participantKey
            ,victimKey : victim.participantKey
            ,move : struggle
            ,attackerID : attacker.pokemon.pokemonID
            ,victimID : victim.pokemon.pokemonID);
        
        SaveTurn(struggleTurn);
    }
    public void SaveSwitchTurn(SwitchOutData data)
    {
        var fakeMove = ScriptableObject.CreateInstance<Move>();
        fakeMove.priority = 0;
            
        var switchTurn = new Turn(TurnUsage.SwitchOut,
            attackerKey: _battleHandler.GetCurrentParticipant().participantKey
            ,move:fakeMove
            ,attackerID:Utility.Random16Bit());
            
        switchTurn.switchData = data;
        SaveTurn(switchTurn);
    }
    private bool IsLastParticipant()
    {
        var livingParticipants = _battleHandler.GetParticipants
            .Where(participant => participant.isActive)
            .Select(p=>p.participantKey).ToList();
        
        var lastKeyInTurnOrder = livingParticipants
            .OrderByDescending(key => key)
            .ToList()
            .First();
        
        if (lastKeyInTurnOrder == _battleHandler.GetCurrentParticipant().participantKey)
        {
            return true;
        }
        return false;
    }
    private void ResetTurnState()
    {
        currentTurnIndex = 0;
        CurrentWeather = clearWeather;
        ClearTurn();
        StopAllCoroutines();
    }
    
    private IEnumerator CheckAttackSuccess(Turn turn, BattleParticipant attacker,BattleParticipant victim)
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
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName + " is confused");
                yield return _battleVisualsHandler.DisplayConfusionVisuals(attacker);
                if (Utility.RandomRange(0, 2) < 1)
                {
                    _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" hurt itself in its confusion");
                    yield return _moveUsageHandler.DealConfusionDamage(attacker);
                    OnAttackAttempted?.Invoke(false);
                    yield break;
                }
            }
            if (attacker.isInfatuated)
            {
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName + " is in love ");
                if (Utility.RandomRange(0, 2) < 1)
                {
                    _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" can’t move because of love");
                    OnAttackAttempted?.Invoke(false);
                    yield break;
                }
            }
            
            if(!attacker.semiInvulnerabilityData.executionTurn && !attacker.currentCoolDown.isCoolingDown)
            {
                _dialogueHandler.DisplayBattleInfo(GetMoveUsageText(turn.move,attacker, victim));
            }
            
            if (victim.isSemiInvulnerable)
            {
                if (turn.move.isSelfTargeted)
                {
                    OnAttackAttempted?.Invoke(true);
                    yield break;
                }
                if (victim.semiInvulnerabilityData.IsInvulnerableTo(turn.move))
                {
                    _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName +
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
                        _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName +
                                                                    " missed the attack");
                    else
                        _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName +
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
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" flinched!");
            else if (attacker.pokemon.statusEffect != StatusEffect.None)
            {
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" is affected by "+ attacker.pokemon.statusEffect);
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
                yield return _dialogueHandler.AwaitAllDialogue();
            }
        }

        if (switchTurns.Count > 0)
        {
            var orderTurns = switchTurns.OrderByDescending(itemIndex=>itemIndex).ToList();//prevent index out of range when removing turns
            orderTurns.ForEach(RemoveTurn);
        }
        
//handle all attacks
        foreach (var currentTurn in _turnHistory )
        {
            if (_battleHandler.BattleOver) break;
            
            if (currentTurn.isCancelled) continue;
            
            currentTurn.turnExecuted = true;
            
            var attacker=_battleHandler.GetParticipant(currentTurn.attackerKey);
            var victim=_battleHandler.GetParticipant(currentTurn.victimKey);
            
            attacker.isSemiInvulnerable = false;
            
            //check on participants
            if (!IsValidParticipantState(attacker)) continue;
            
            if (!IsValidParticipant(currentTurn,attacker)) continue;
            
            if (!IsValidParticipantState(victim))
            {
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" missed the attack");
                yield return _dialogueHandler.AwaitAllDialogue();
                continue;
            }
            
            yield return OnMoveExecute?.Invoke(attacker);
           
            yield return attacker.heldItemHandler.CheckForUsableItem();

            yield return _dialogueHandler.AwaitAllDialogue();

            if (currentTurn.turnUsage == TurnUsage.UseStruggle)
            {
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName + " is out of moves");
                yield return _dialogueHandler.AwaitAllDialogue();
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName + " used struggle");
                yield return _moveUsageHandler.DealStruggleDamage(victim,attacker,currentTurn.move);
                yield return _battleHandler.AwaitFaintQueue();
                yield return _dialogueHandler.AwaitAllDialogue();
                continue;
            }
            
            successfulAttack = false;
            OnAttackAttempted += GetAttackResult;
            
            yield return CheckAttackSuccess(currentTurn,attacker,victim);
            yield return _dialogueHandler.AwaitAllDialogue();
            
            yield return _battleHandler.AwaitFaintQueue();
            CheckRepeatedMove(attacker,currentTurn.move);
            if (successfulAttack)
            {
                _moveUsageHandler.BeginMoveExecution(currentTurn);

                yield return _moveUsageHandler.AwaitMoveCompletion();
                
                yield return _battleHandler.AwaitFaintQueue();
            }
            else
            {
                attacker.previousMoveData.failedAttempt = true;
                attacker.semiInvulnerabilityData.ResetState();
            }
        }
        yield return _battleHandler.AwaitFaintQueue();
        yield return _dialogueHandler.AwaitAllDialogue();

        ClearTurn();
        OnTurnsCompleted?.Invoke();
        
        var validList = _battleHandler.GetValidParticipants();
        
        foreach (var participant in validList)
        {
            yield return participant.statusHandler.CheckStatus();
        }
        yield return _battleHandler.AwaitFaintQueue();
        
        //damage from weather
        if (CurrentWeather.weather != Weather.Clear)
        {
            ReduceWeatherDuration();
            yield return ExecuteWeatherEffect();
        }
        yield return _battleHandler.AwaitFaintQueue();
        
        yield return _dialogueHandler.AwaitAllDialogue();
        
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
                        participant.currentCoolDown.isExecutionTurn = true;
                        AddTurn(new(participant.currentCoolDown.turnData));
                    }
                }
            }
            
            participant.pokemon.ResetMoveData();
            
            if(!participant.isSemiInvulnerable)continue;
            
            AddTurn(new Turn(participant.semiInvulnerabilityData.turnData));
        }
        
        yield return _dialogueHandler.AwaitAllDialogue();
        NextTurn();
    }
    public IEnumerator AllowPlayerSwitchIn(string trainerName,string pokemonName)
    {
        if (_battleHandler.currentBattleStyle != BattleHandler.BattlesStyle.Switch) yield break;
        if (_battleHandler.isDoubleBattle)
        {
//only happens in single battles
            yield break;
        }
        yield return _dialogueHandler.AwaitAllDialogue();
        
        _dialogueHandler.DisplayCustomOptions($"{trainerName} is about to use {pokemonName}, change pokemon?",
            new[] { "Yes", "No" }, new Action[] { SwitchAccepted, SwitchRejected });
        
        bool processing = true;
        yield return new WaitUntil(() => !processing);

        void SwitchRejected()
        {
            processing = false;
        }
        void SwitchAccepted()
        {
            _battleHandler.OnSwitchIn += ResetEvent;
            _battleHandler.GetParticipant(BattleParticipantKey.Player).SetupSwitchOut();
        }
        void ResetEvent()
        {
            _battleHandler.OnSwitchIn -= ResetEvent;
            processing = false;
        }
    }
    public IEnumerator HandleSwap(SwitchOutData swap, bool forcedSwap=false)
    {
        if (forcedSwap)
        {
            _dialogueHandler.DisplayBattleInfo(swap.participant.pokemon.pokemonDisplayName
                                                        + " was blown out");
        }
        else
        {
            if (swap.participant.isPlayer)
            {
                _dialogueHandler.DisplayBattleInfo(_gameLoadingHandler.playerData.playerName
                                                            + " withdrew " + swap.participant.pokemon.pokemonDisplayName);
            }
            else
            {
                _dialogueHandler.DisplayBattleInfo(swap.participant.pokemonTrainerAI.trainerData.TrainerName
                                                            +" withdrew "+swap.participant.pokemon.pokemonDisplayName);
            }
        }
        
        if (swap.participant.isPlayer || forcedSwap)
        {
            yield return _battleVisualsHandler.WithdrawPokemon(swap.participant);
        }
        //check if move used was pursuit
        var pursuitUsersTurn = _turnHistory.FirstOrDefault(turn => 
            turn.move.moveName == NameDB.GetMoveName(LearnSetMoveName.Pursuit));
        
        if(pursuitUsersTurn is { turnExecuted: false })
        {
            var attacker=_battleHandler.GetParticipant(pursuitUsersTurn.attackerKey);
            var victim=_battleHandler.GetParticipant(pursuitUsersTurn.victimKey);
            
            if (victim == swap.participant)
            {
                bool pokemonFainted = false;

                pursuitUsersTurn.isCancelled = true;//since it strikes here
                
                _battleHandler.OnParticipantFainted += CancelOnFaint;
                
                void CancelOnFaint(BattleParticipant faintedParticipant)
                {
                    if (faintedParticipant != victim) return;
                    _battleHandler.OnParticipantFainted -= CancelOnFaint;
                    pokemonFainted = true;
                }
                
                yield return _moveLogicHandler.Pursuit(attacker, victim, pursuitUsersTurn.move); 
                
                if (pokemonFainted) yield break;
                
                _battleHandler.OnParticipantFainted -= CancelOnFaint;
            }
        }
        
        if (swap.participant.isPlayer)
        {
            swap.participant.ResetParticipantState();
            var party = _pokemonPartyHandler.Party;
            _pokemonPartyHandler.SwapIndexes(swap.partyPosition, swap.memberToSwapWith);
            _pokemonPartyHandler.UpdateUIAfterSwap();
            
            _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
            yield return _battleIntroHandler.SwitchInPokemon(swap.participant,party[swap.partyPosition]);
        }
        else
        {
            swap.participant.ResetParticipantState();
            var enemyParty = swap.participant.pokemonTrainerAI.trainerParty;
            (enemyParty[swap.partyPosition], enemyParty[swap.memberToSwapWith]) = (enemyParty[swap.memberToSwapWith], enemyParty[swap.partyPosition]);
            yield return _battleIntroHandler.SwitchInPokemon(swap.participant,enemyParty[swap.partyPosition]);
        }
    }

    public bool ContainsSwitch(int memberPosition)
    {
        foreach (var turn in _turnHistory)
        {
            if (turn.turnUsage==TurnUsage.SwitchOut)//must check this first to avoid null ref
            {
                if (turn.switchData.memberToSwapWith==memberPosition)
                {
                    return true;
                }
            }
        }
        return false;
    }
    public string GetMoveUsageText(Move move, BattleParticipant attacker,BattleParticipant victim)
    {
        if (move.displayTargetMessage)
            return attacker.pokemon.pokemonDisplayName + " used " 
                                                + move.moveName + " on " + victim.pokemon.pokemonDisplayName + "!";
        return attacker.pokemon.pokemonDisplayName + " used " + move.moveName + "!";
    }

    private IEnumerator CheckParticipantCoolDown()
    {
        if (_battleHandler.BattleOver) yield break;
        var participant = _battleHandler.GetCurrentParticipant();
        if (!participant.currentCoolDown.isCoolingDown) yield break;
        if (participant.currentCoolDown.isExecutionTurn)
        {
            NextTurn();
            yield break;
        }
        
        if (participant.currentCoolDown.canDisplayMessage)
        {
            _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName
                                                        +participant.currentCoolDown.message);
            yield return _dialogueHandler.AwaitAllDialogue();
        }
        participant.currentCoolDown.numTurns--;
        NextTurn();
    }
    private void CheckRepeatedMove(BattleParticipant attacker, Move move)
    {
        if (attacker.previousMoveData==null)
        {
            var newData = new PreviousMove(move,0);
            attacker.previousMoveData = newData;
            return;
        }

        if (attacker.previousMoveData.move.moveName == move.moveName)
        {
            attacker.previousMoveData.numRepetitions++;
            attacker.previousMoveData.failedAttempt = false;
        }
        else
        {
            var newData = new PreviousMove(move,0);
            attacker.previousMoveData = newData;
        }
    }
    private bool IsValidParticipantState(BattleParticipant participant)
    {
        if (!participant.isActive) return false;
        return participant.pokemon is { hp: > 0 };
    }

    private bool IsValidParticipant(Turn turn,BattleParticipant participant)
    {
        return turn.attackerID == participant.pokemon.pokemonID||
                turn.victimID == participant.pokemon.pokemonID;
    }

    public void StartFreshTurn()
    {
        currentTurnIndex = 0;
        OnNewTurn?.Invoke();
    }
    public void NextTurn()
    {
        if (_battleHandler.isDoubleBattle)
            ChangeTurn(3, 1);
        else
            ChangeTurn(2, 2);
    }

    public void RemoveTurn()
    {
        //player wants to change their turn usage
        RemoveTurn(currentTurnIndex-1);
        currentTurnIndex --;
        _inputStateHandler.OnStateRemoved += _battleHandler.SetupOptionsAfterTurnReset;
    }
    private void ChangeTurn(int maxParticipantIndex,int step)
    {
        if (currentTurnIndex < maxParticipantIndex)
            currentTurnIndex+=step;
        else
            currentTurnIndex = 0;
        
        if (!_battleHandler.GetCurrentParticipant().isActive)
        {
            if (_battleHandler.isDoubleBattle && IsLastParticipant())
            {
                BeginTurnExecution();
            }
            else
            {
                NextTurn();
            }
        }
        else
        {
            OnNewTurn?.Invoke();
        }
    }
    private bool MoveSuccessful(Turn turn)
    {
        var random = Utility.RandomRange(1, 100);
        var hitChance = turn.move.moveAccuracy *
                           (_battleHandler.GetParticipant(turn.attackerKey).pokemon.accuracy / 
                            _battleHandler.GetParticipant(turn.victimKey).pokemon.evasion);
        return hitChance>random;
    }
    private void SetPriority()
    {
        var orderBySpeed = _turnHistory
            .OrderByDescending(turn => 
                _battleHandler.GetParticipant(turn.attackerKey).pokemon.speed).ToList();
        
        var priorityList = orderBySpeed.OrderByDescending(turn => turn.move.priority).ToList();
        ClearTurn();
        _turnHistory.AddRange(priorityList);
    }

    public void ChangeWeather(WeatherCondition newWeather,bool fromAbility=false)
    {
        OnWeatherEffect -= CurrentWeather.weatherEffect;
        OnWeatherEnd?.Invoke();//clean weather damage modifications
        
        if (fromAbility)
            newWeather.isInfinite = true;
        else
            newWeather.turnDuration = 5;
        
        switch (newWeather.weather)
        {
            case Weather.Sandstorm:
                OnWeatherEnd += ResetWeatherBuffs;
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
        CurrentWeather = newWeather;
    }
    private void ResetWeatherBuffs()
    {
        OnWeatherEnd -= ResetWeatherBuffs;
        foreach (var participant in CurrentWeather.buffedParticipants)
        {
            var spDefBuff = new StatChangeTransitData(participant, Stat.SpecialDefense, false, 1);
            _moveUsageHandler.InitiateStatChange(spDefBuff,false);
        }
        CurrentWeather.buffedParticipants.Clear();
    }
    private void ReduceWeatherDuration()
    {
        if (CurrentWeather.isInfinite) return;
        if (CurrentWeather.turnDuration == 0)
        {
            OnWeatherEnd?.Invoke();
            OnWeatherEffect -= CurrentWeather.weatherEffect;
            _dialogueHandler.DisplayBattleInfo(CurrentWeather.weatherEndMessage);
            CurrentWeather = clearWeather;
            return;
        }
        CurrentWeather.turnDuration--;
    }

    private IEnumerator ExecuteWeatherEffect()
    {
        _dialogueHandler.DisplayBattleInfo(CurrentWeather.weatherTurnEndMessage);
        yield return OnWeatherEffect?.Invoke();
    }

    private void RemoveWeatherBuffReceiver(BattleParticipant participant)
    {
        if (CurrentWeather.weather == Weather.Clear) return;
        if (!CurrentWeather.buffedParticipants.Contains(participant)) return;
        CurrentWeather.buffedParticipants.Remove(participant);
    }
    private IEnumerator SandStormEffect()
    {
        var protectedTypes = new[]{
            PokemonType.Rock, PokemonType.Ground, PokemonType.Steel
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
                    
                    var participantIsBuffed = CurrentWeather.buffedParticipants
                        .Any(p=>p.participantKey == participant.participantKey);
                    
                    if(!participantIsBuffed)
                    {
                        if (protectedType == PokemonType.Rock)
                        {
                            //buff rock types
                            var spDefBuff = new StatChangeTransitData(participant,
                                Stat.SpecialDefense, true, 1);
                            _moveUsageHandler.InitiateStatChange(spDefBuff,false);
                            CurrentWeather.buffedParticipants.Add(participant);
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
        if (_moveUsageHandler.FieldDamageSourceExists(DamageModifierSource.Rain))
        {
            yield break;
        }
        DamageModifierForWeather( PokemonType.Fire,0.5f,DamageModifierSource.Rain);
        DamageModifierForWeather( PokemonType.Water,1.5f,DamageModifierSource.Rain);
        yield return null;
    }
    private IEnumerator SunEffect()
    {
        if (_moveUsageHandler.FieldDamageSourceExists(DamageModifierSource.Sunlight))
        {
            yield break;
        }
        DamageModifierForWeather(PokemonType.Fire,1.5f,DamageModifierSource.Sunlight);
        DamageModifierForWeather(PokemonType.Water,0.5f,DamageModifierSource.Sunlight);
        yield return null;
    }
    private IEnumerator HailEffect()
    {
        var validParticipants = _battleHandler.GetValidParticipants();
        foreach (var participant in validParticipants)
        {
            if (participant.pokemon.HasType(PokemonType.Ice)) continue;
            yield return DealWeatherDamage(participant);
        }
    }

    private void DamageModifierForWeather(PokemonType type,float damageFactor,DamageModifierSource source)
    { 
        var damageModifierInfo = ScriptableObject.CreateInstance<DamageModifierInfo>();
        var damageModForType = new DamageModifierForType(type,damageFactor);
        damageModifierInfo.damageModifiers.Add(damageModForType);
        damageModifierInfo.modifierSource = source;
        
        var fieldModifier = new OnFieldDamageModifier(_battleHandler,_moveUsageHandler,
            this
            ,damageModifierInfo,removeOnSwitch:false);
        
        OnWeatherEnd += fieldModifier.RemoveAfterWeather;
        _moveUsageHandler.AddFieldDamageModifier(fieldModifier);
    }
    private IEnumerator DealWeatherDamage(BattleParticipant victim)
    {
        _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName + CurrentWeather.weatherDamageMessage);
        var weatherDamage = victim.pokemon.maxHp * (1 / 16f);
        _moveUsageHandler.DisplaySpecialDamage(victim, predefinedDamage:weatherDamage);
        yield return _moveUsageHandler.AwaitDamageDisplay();
    }
}
