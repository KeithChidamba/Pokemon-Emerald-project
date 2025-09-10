using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class Turn_Based_Combat : MonoBehaviour
{
    public static Turn_Based_Combat Instance; 
    [SerializeField]List<Turn> _turnHistory = new();
    public event Action OnNewTurn;
    public event Action<Battle_Participant> OnMoveExecute;
    public event Action OnTurnsCompleted;
    public int currentTurnIndex = 0;

    public bool faintEventDelay = false;
    public WeatherCondition currentWeather;
    public WeatherCondition clearWeather;
    private event Func<IEnumerator> OnWeatherEffect;
    public event Action OnWeatherEnd;
    private List<Battle_Participant> _statusCheckQueue = new();
    private List<Held_Items> _heldItemUsageQueue = new();
    private event Action<bool> OnAttackAttempted;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        Battle_handler.Instance.OnBattleEnd += ResetTurnState;
        OnNewTurn += AllowPlayerInput;
        OnNewTurn += ()=> StartCoroutine(CheckParticipantCoolDown());
        Battle_handler.Instance.OnSwitchOut += RemoveWeatherBuffReceiver;
        Battle_handler.Instance.OnBattleStart += AddParticipantQueues;
        clearWeather = new WeatherCondition(WeatherCondition.Weather.Clear);
        currentWeather = clearWeather;
    }

    private void AddParticipantQueues()
    {
        Battle_handler.Instance.OnBattleStart -= AddParticipantQueues;
        foreach(var participant in Battle_handler.Instance.battleParticipants)
        {
            participant.statusHandler.OnStatusCheck += p => _statusCheckQueue.Add(p);
            participant.heldItemHandler.OnHeldItemUsage +=
                heldItemHandler=>_heldItemUsageQueue.Add(heldItemHandler);
        }
    }
    public void SaveTurn(Turn turn)
    {
        _turnHistory.Add(turn);
        if ((Battle_handler.Instance.isDoubleBattle && IsLastParticipant())
            || (currentTurnIndex == Battle_handler.Instance.participantCount))
        {
            InputStateHandler.Instance.AddPlaceHolderState();
            SetPriority();
            StartCoroutine(ExecuteMoves());    
        }
        else
        {
            InputStateHandler.Instance.ResetRelevantUi(new[]{InputStateHandler.StateName.PokemonBattleMoveSelection
                ,InputStateHandler.StateName.PokemonBattleEnemySelection});
            NextTurn();
        }
    }

    private bool IsLastParticipant()
    {
        var livingParticipants = Battle_handler.Instance.battleParticipants.ToList();
        livingParticipants.RemoveAll(participant => participant.pokemon==null);
        if (livingParticipants.Last() ==
            Battle_handler.Instance.battleParticipants[currentTurnIndex])
            return true;
        return false;
    }
    private void ResetTurnState()
    {
        currentTurnIndex = 0;
        currentWeather.weather = WeatherCondition.Weather.Clear;
        _turnHistory.Clear();
        faintEventDelay = false;
        StopAllCoroutines();
    }

    private void ModifyMoveAccuracy(Turn turn)
    {
        if (turn.move.moveName == NameDB.GetMoveName(NameDB.LearnSetMove.Thunder))
        {
            if (currentWeather.weather == WeatherCondition.Weather.Rain)
                turn.move.isSureHit = true;
            if (currentWeather.weather == WeatherCondition.Weather.Sunlight)
                turn.move.moveAccuracy = 50f;
        }
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
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName + " is confused");
                if (Utility.RandomRange(0, 2) < 1)
                {
                    Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" hurt itself in its confusion");
                    yield return Move_handler.Instance.DealConfusionDamage(attacker);
                    OnAttackAttempted?.Invoke(false);
                    yield break;
                }
            }
            if (attacker.isInfatuated)
            {
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName + " is in love ");
                if (Utility.RandomRange(0, 2) < 1)
                {
                    Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" can’t move because of love");
                    OnAttackAttempted?.Invoke(false);
                    yield break;
                }
            }
            
            if(!attacker.semiInvulnerabilityData.executionTurn && !attacker.currentCoolDown.isCoolingDown)
            {
                Dialogue_handler.Instance.DisplayBattleInfo(GetMoveUsageText(turn.move,attacker, victim));
            }

            ModifyMoveAccuracy(turn);
            
            if (victim.isSemiInvulnerable)
            {
                if (victim.semiInvulnerabilityData.IsInvulnerableTo(turn.move))
                {
                    Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName +
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
                        Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName +
                                                                    " missed the attack");
                    else
                        Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName +
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
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" flinched!");
            else if(attacker.pokemon.statusEffect!=PokemonOperations.StatusEffect.None)
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" is affected by "+ attacker.pokemon.statusEffect);
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
            if (_turnHistory[i].turnUsage == Turn.TurnUsage.SwitchOut)
            {
                switchTurns.Add(i);
                yield return HandleSwap(_turnHistory[i].switchData);
                yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
            }
        }
        
        switchTurns.ForEach(index => _turnHistory.RemoveAt(index));
        
//handle all attacks
        foreach (var currentTurn in _turnHistory )
        {
            if (Battle_handler.Instance.battleOver) break;

            if (currentTurn.isCancelled) continue;
            
            currentTurn.turnExecuted = true;
            
            var attacker=Battle_handler.Instance.battleParticipants[currentTurn.attackerIndex];
            var victim=Battle_handler.Instance.battleParticipants[currentTurn.victimIndex];
            
            attacker.isSemiInvulnerable = false;
            
            //check on participants
            if (!IsValidParticipantState(attacker))
                continue;
            
            if (!IsValidParticipant(currentTurn,attacker))
                continue;
            
            if (!IsValidParticipantState(victim))
            {
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" missed the attack");
                yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
                continue;
            }
           
            OnMoveExecute?.Invoke(attacker);
            
            //processing held item effect
            while (_heldItemUsageQueue.Count > 0)
            {
                yield return new WaitUntil( ()=> !_heldItemUsageQueue[0].processingItemEffect);
                _heldItemUsageQueue.RemoveAt(0);
                yield return null;
            }
            yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
            
            successfulAttack = false;
            OnAttackAttempted += GetAttackResult;
            
            yield return CheckAttackSuccess(currentTurn,attacker,victim);
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
            
            if (successfulAttack)
            {
                yield return new WaitUntil(() => Battle_handler.Instance.faintQueue.Count == 0 && !faintEventDelay);
                
                Move_handler.Instance.doingMove = true;
                CheckRepeatedMove(attacker,currentTurn.move);
                Move_handler.Instance.ExecuteMove(currentTurn);
                
                yield return new WaitUntil(() => !Move_handler.Instance.doingMove);
                yield return new WaitUntil(() => Battle_handler.Instance.faintQueue.Count == 0 && !faintEventDelay);
            }
            else
                attacker.semiInvulnerabilityData.ResetState();
        }
        yield return new WaitUntil(() => Battle_handler.Instance.faintQueue.Count == 0 && !faintEventDelay);
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
        
        _turnHistory.Clear();
        OnTurnsCompleted?.Invoke();
        
        //damage from status effect
        while (_statusCheckQueue.Count > 0)
        {
            yield return new WaitUntil( ()=> !_statusCheckQueue[0].statusHandler.dealingStatusDamage);
            _statusCheckQueue.RemoveAt(0);
            yield return null;
        }
        yield return new WaitUntil(() => Battle_handler.Instance.faintQueue.Count == 0 && !faintEventDelay);
        
        //damage from weather
        if (currentWeather.weather != WeatherCondition.Weather.Clear)
        {
            ReduceWeatherDuration();
            yield return ExecuteWeatherEffect();
        }
        yield return new WaitUntil(() => Battle_handler.Instance.faintQueue.Count == 0 && !faintEventDelay);
        
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
        
        //semi-invulnerability turn logic and cooldown check
        var validList = Battle_handler.Instance.GetValidParticipants();
        foreach(var participant in validList)
        {
            if (participant.currentCoolDown.isCoolingDown)
            {
                if (participant.currentCoolDown.NumTurns == 0)
                {
                    if (participant.currentCoolDown.turnData.isCancelled)
                    {
                        participant.currentCoolDown.ResetState();
                    }
                    else
                    {
                        participant.currentCoolDown.ExecuteTurn = true;
                        _turnHistory.Add(new(participant.currentCoolDown.turnData));
                        NextTurn();
                    }
                }
            }
            
            participant.pokemon.ResetMoveData();
            
            if(!participant.isSemiInvulnerable)continue;
            
            _turnHistory.Add((new Turn(participant.semiInvulnerabilityData.turnData)));
        }
        
        NextTurn();
    }
    public IEnumerator HandleSwap(SwitchOutData swap, bool forcedSwap=false)
    {
        if (forcedSwap)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(swap.Participant.pokemon.pokemonName
                                                        + " was blown out");
        }
        else
        {
            if (swap.IsPlayer)
            {
                Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName
                                                            + " withdrew " + swap.Participant.pokemon.pokemonName);
            }
            else
            {
                Dialogue_handler.Instance.DisplayBattleInfo(swap.Participant.pokemonTrainerAI.trainerData.TrainerName
                                                            +" withdrew "+swap.Participant.pokemon.pokemonName);
            }
        }

        yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);

        //check if move used was pursuit
        var pursuitUsersTurn = _turnHistory.FirstOrDefault(turn => 
            turn.move.moveName == NameDB.GetMoveName(NameDB.LearnSetMove.Pursuit));
        
        if(pursuitUsersTurn is { turnExecuted: false })
        {
            var attacker=Battle_handler.Instance.battleParticipants[pursuitUsersTurn.attackerIndex];
            var victim=Battle_handler.Instance.battleParticipants[pursuitUsersTurn.victimIndex];
            
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

                yield return MoveLogicHandler.Instance.Pursuit(attacker, victim, pursuitUsersTurn.move);
                if (pokemonFainted) yield break;
            }
        }
        
        if (swap.IsPlayer)
        {
            swap.Participant.ResetParticipantState();
            Pokemon_party.Instance.SwitchInMemberSwap(swap);
            InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonParty);
        }
        else
        {
            Battle_handler.Instance.SetParticipant(swap.Participant
                ,newPokemon:swap.Participant.pokemonTrainerAI.trainerParty[swap.MemberToSwapWith]);
        }
    }

    public bool ContainsSwitch(int memberPosition)
    {
        foreach (var turn in _turnHistory)
        {
            if (turn.turnUsage==Turn.TurnUsage.SwitchOut)//must check this first to avoid null ref
            {
                if (turn.switchData.MemberToSwapWith==memberPosition)
                {
                    return true;
                }
            }
        }
        return false;
    }
    private void AllowPlayerInput()
    {
        if (currentTurnIndex > 1) return;
        var currentParticipant = Battle_handler.Instance.GetCurrentParticipant();
        if (currentParticipant.isSemiInvulnerable) return;
        if (currentParticipant.currentCoolDown.isCoolingDown) return;
        InputStateHandler.Instance.ResetRelevantUi(new[]{ InputStateHandler.StateName.DialoguePlaceHolder});
        InputStateHandler.Instance.ResetRelevantUi(new[]{InputStateHandler.StateName.PokemonBattleEnemySelection,
            InputStateHandler.StateName.PlaceHolder});
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
        var participant = Battle_handler.Instance.GetCurrentParticipant();
        if (!participant.currentCoolDown.isCoolingDown) yield break;
        if (participant.currentCoolDown.ExecuteTurn) yield break;
        
        if (participant.currentCoolDown.DisplayMessage)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(participant.pokemon.pokemonName
                                                        +participant.currentCoolDown.Message);
            yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
        }
        participant.currentCoolDown.NumTurns--;
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
            attacker.previousMove.numRepetitions++;
        else
        {
            var newData = new PreviousMove(move,0);
            attacker.previousMove = newData;
        }
    }
    private bool IsValidParticipantState(Battle_Participant participant)
    {
        if (!participant.isActive) return false;
        if (participant.pokemon.hp<=0) return false;
        return participant.pokemon != null;
    }

    private bool IsValidParticipant(Turn turn,Battle_Participant participant)
    {
        return turn.attackerID == participant.pokemon.pokemonID||
                turn.victimID == participant.pokemon.pokemonID;
    }
    public void NextTurn()
    {
        if (Battle_handler.Instance.isDoubleBattle)
            ChangeTurn(3, 1);
        else
            ChangeTurn(2, 2);

        if (Battle_handler.Instance.GetCurrentParticipant().isSemiInvulnerable)
            NextTurn();
    }

    public void RemoveTurn()
    {
        //player wants to change their turn usage
        _turnHistory.RemoveAt(currentTurnIndex-1);
        currentTurnIndex --;
        InputStateHandler.Instance.OnStateRemovalComplete += Battle_handler.Instance.SetupOptionsInput;
    }
    public void ChangeTurn(int maxParticipantIndex,int step)
    {
        if (currentTurnIndex < maxParticipantIndex)
            currentTurnIndex+=step;
        else
            currentTurnIndex = 0;

        OnNewTurn?.Invoke();
        
        if (!Battle_handler.Instance.battleParticipants[currentTurnIndex].isActive & Options_manager.Instance.playerInBattle)
            NextTurn();
        
    }
    private bool MoveSuccessful(Turn turn)
    {
        var random = Utility.RandomRange(1, 100);
        var hitChance = turn.move.moveAccuracy *
                           (Battle_handler.Instance.battleParticipants[turn.attackerIndex].pokemon.accuracy / 
                            Battle_handler.Instance.battleParticipants[turn.victimIndex].pokemon.evasion);
        return hitChance>random;
    }
    private void SetPriority()
    {
        var orderBySpeed = _turnHistory.OrderByDescending(p => Battle_handler.Instance.battleParticipants[p.attackerIndex].pokemon.speed).ToList();
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
            case WeatherCondition.Weather.Sandstorm:
                newWeather.weatherEffect = SandStormEffect;
                newWeather.weatherBegunMessage = "A sandstorm brewed!";
                newWeather.weatherTurnEndMessage = "The sandstorm rages.";
                newWeather.weatherDamageMessage = " is buffeted by the sandstorm!";
                newWeather.weatherEndMessage = "The sandstorm subsided.";
                break;
            case WeatherCondition.Weather.Rain:
                newWeather.weatherEffect = RainEffect;
                newWeather.weatherBegunMessage = "It started to rain!";
                newWeather.weatherTurnEndMessage = "Rain continues to fall.";
                newWeather.weatherEndMessage = "The rain stopped.";
                break;
            case WeatherCondition.Weather.Hail:
                newWeather.weatherEffect = HailEffect;
                newWeather.weatherBegunMessage = "It started to hail!";
                newWeather.weatherTurnEndMessage = "Hail continues to fall.";
                newWeather.weatherDamageMessage = " is pelted by hail!";
                newWeather.weatherEndMessage = "The hail stopped.";
                break;
            case WeatherCondition.Weather.Sunlight:
                newWeather.weatherEffect = SunEffect;
                newWeather.weatherBegunMessage = "The sunlight got bright!";
                newWeather.weatherTurnEndMessage = "The sunlight is strong.";
                newWeather.weatherEndMessage = "The sunlight faded.";
                break;
        }
        Dialogue_handler.Instance.DisplayBattleInfo(newWeather.weatherBegunMessage);
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
            Dialogue_handler.Instance.DisplayBattleInfo(currentWeather.weatherEndMessage);
            currentWeather = clearWeather;
            return;
        }
        currentWeather.turnDuration--;
    }

    private IEnumerator ExecuteWeatherEffect()
    {
        Dialogue_handler.Instance.DisplayBattleInfo(currentWeather.weatherTurnEndMessage);
        yield return OnWeatherEffect?.Invoke();
    }

    private void RemoveWeatherBuffReceiver(Battle_Participant participant)
    {
        if (currentWeather.weather == WeatherCondition.Weather.Clear) return;
        if (!currentWeather.buffedParticipants.Contains(participant)) return;
        currentWeather.buffedParticipants.Remove(participant);
    }
    private IEnumerator SandStormEffect()
    {
        var protectedTypes = new[]{
            PokemonOperations.Types.Rock, PokemonOperations.Types.Ground, PokemonOperations.Types.Steel
        };
        var validParticipants = Battle_handler.Instance.GetValidParticipants();
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
                        if (protectedType == PokemonOperations.Types.Rock)
                        {
                            //buff rock types
                            var spDefBuff = new BuffDebuffData(participant,
                                PokemonOperations.Stat.SpecialDefense, true, 1);
                            BattleOperations.CanDisplayDialougue = false;
                            Move_handler.Instance.SelectRelevantBuffOrDebuff(spDefBuff);
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
        DamageModifierForWeather( PokemonOperations.Types.Fire,0.5f);
        DamageModifierForWeather( PokemonOperations.Types.Water,1.5f);
        yield return null;
    }
    private IEnumerator SunEffect()
    {
        DamageModifierForWeather( PokemonOperations.Types.Fire,1.5f);
        DamageModifierForWeather( PokemonOperations.Types.Water,0.5f);
        yield return null;
    }
    private IEnumerator HailEffect()
    {
        var validParticipants = Battle_handler.Instance.GetValidParticipants();
        foreach (var participant in validParticipants)
        {
            if (participant.pokemon.HasType(PokemonOperations.Types.Ice)) continue;
            yield return DealWeatherDamage(participant);
        }
    }

    private void DamageModifierForWeather(PokemonOperations.Types type,float damageModifier)
    { 
        var damageModifierInfo = ScriptableObject.CreateInstance<DamageModifierInfo>();
        damageModifierInfo.typeAffected = type;
        damageModifierInfo.damageModifier = damageModifier;
        var modifier = new OnFieldDamageModifier(damageModifierInfo,removeOnSwitch:false);
        OnWeatherEnd += modifier.RemoveAfterWeather;
        Move_handler.Instance.AddFieldDamageModifier(modifier);
    }
    private IEnumerator DealWeatherDamage(Battle_Participant victim)
    {
        Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName + currentWeather.weatherDamageMessage);
        var weatherDamage = victim.pokemon.maxHp * (1 / 16f);
        Move_handler.Instance.DisplayDamage(victim,isSpecificDamage:true,
            predefinedDamage:weatherDamage,displayEffectiveness:false);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingDamage);
    }
}
