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
    
    private List<Func<BattleParticipant, IEnumerator>> _moveExecutionHandlers = new();
    
    public event Action OnTurnsCompleted;
    public event Action OnTurnEventsCompleted;
    
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
    private GameLoadingHandler _gameLoadingHandler;
    private WildPokemonAiHandler _wildPokemonAI;
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
        _wildPokemonAI = container.Resolve<WildPokemonAiHandler>();
        
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _battleHandler.OnBattleEnd += ResetTurnState;
        OnNewTurn += ResolveTurnStateForParticipants;
        _battleHandler.OnSwitchOut += RemoveWeatherBuffReceiver;
        clearWeather = new WeatherCondition(Weather.Clear);
        CurrentWeather = clearWeather;
    }
    
    public void SubToMoveExecution(Func<BattleParticipant,IEnumerator> subscriber)
    {
        if (!_moveExecutionHandlers.Contains(subscriber))
        {
            _moveExecutionHandlers.Add(subscriber);
        }
        else
        {
            Debug.LogError("Duplicate Subscriber for move execution");
        } 
    }
    public void UnsubscribeFromMoveExecution(Func<BattleParticipant,IEnumerator> subscriber)
    {
        _moveExecutionHandlers.Remove(subscriber);
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
    private bool IsLastToAct()
    {
        return (_battleHandler.isDoubleBattle && IsLastParticipant())
               || currentTurnIndex == _battleHandler.validParticipantCount;
    }
    private void AdvanceAfterAutomaticTurn()
    {
        if (IsLastToAct())
            BeginTurnExecution();
        else
            NextTurn();
    }
    private static void AbortSemiInvulnerableState(BattleParticipant participant)
    {
        participant.isSemiInvulnerable = false;
        participant.semiInvulnerabilityData?.ResetState();
    }
    private void BeginTurnExecution()
    {
        _inputStateHandler.AddPlaceHolderState();
        StartCoroutine(ExecuteMoves()); 
    }
    public void SaveTurn(Turn turn)
    {
        if (_turnHistory.Any(t => t.attackerID == turn.attackerID
                                  || turn.attackerKey == t.attackerKey))
        {
            //for testing incase new bug
            Debug.Log("duplicate detected, index of : " + turn.attackerKey);
            return;
        }
        AddTurn(turn);
        _battleHandler.SetPlayerTurnUsage(PlayerTurnUsage.Fight);
        if (IsLastToAct())
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
        var typelessType = ScriptableObject.CreateInstance<PokemonTypeData>();
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
            ,attackerID : attacker.Pokemon.pokemonID
            ,victimID : victim.Pokemon.pokemonID);
        
        SaveTurn(struggleTurn);
    }
    public void SaveSwitchTurn(SwitchOutData data)
    {
        var switchTurn = new Turn(TurnUsage.SwitchOut,
            attackerKey: _battleHandler.GetCurrentParticipant().participantKey
            ,attackerID:Utility.Random16Bit());
            
        switchTurn.switchData = data;
        SaveTurn(switchTurn);
    }
    public void SaveEmptyTurn(BattleParticipantKey participantKey)
    {
        var switchTurn = new Turn(TurnUsage.Empty,
            attackerKey: participantKey
            ,attackerID:Utility.Random16Bit());

        switchTurn.isCancelled = true;
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
        _moveExecutionHandlers.Clear();
        StopAllCoroutines();
    }
    public bool CanMoveReachTarget(Move move, BattleParticipant target, out bool isSureHit)
    {
        return target.IsReachableBy(move, out isSureHit);
    }
    private IEnumerator CheckAttackSuccess(Turn turn, BattleParticipant attacker, BattleParticipant victim)
    {
        if (attacker.Pokemon.hp <= 0)
        {
            OnAttackAttempted?.Invoke(false);
            yield break;
        }
        if (attacker.canAttack)
        {
            if (attacker.isConfused)
            {
                _dialogueHandler.DisplayBattleInfo(attacker.Pokemon.pokemonDisplayName + " is confused");
                yield return _battleVisualsHandler.DisplayConfusionVisuals(attacker);
                if (attacker.statusHandler.AffectedByMinorStatus())
                {
                    _dialogueHandler.DisplayBattleInfo(attacker.Pokemon.pokemonDisplayName + " hurt itself in its confusion");
                    yield return _moveUsageHandler.DealConfusionDamage(attacker);
                    OnAttackAttempted?.Invoke(false);
                    yield break;
                }
            }
            if (attacker.isInfatuated)
            {
                _dialogueHandler.DisplayBattleInfo(attacker.Pokemon.pokemonDisplayName + " is in love ");
                if (attacker.statusHandler.AffectedByMinorStatus())
                {
                    _dialogueHandler.DisplayBattleInfo(attacker.Pokemon.pokemonDisplayName + " can’t move because of love");
                    OnAttackAttempted?.Invoke(false);
                    yield break;
                }
            }

            var onSemiHitTurn = attacker.semiInvulnerabilityData.executionTurn;

            if (!onSemiHitTurn && !attacker.currentCoolDown.isCoolingDown)
            {
                _dialogueHandler.DisplayBattleInfo(GetMoveUsageText(turn.move, attacker, victim));
            }

            //charge turn: always succeeds, target/accuracy are only checked on the hit turn
            var isChargeTurn = turn.move.effectType == EffectType.SemiInvulnerable && !onSemiHitTurn;
            if (isChargeTurn)
            {
                OnAttackAttempted?.Invoke(true);
                yield break;
            }

            var isSureHit = turn.move.isSureHit;
            var accuracyTarget = victim;

            if (turn.move.isMultiTarget)
            {
                //MULTI-TARGET FIX: for these moves `victim` is just whichever enemy the target
                //selection auto-picked, so it must not decide the outcome on its own.
                //The move works if it can reach at least one of its real targets.
                var candidates = GetMultiTargetCandidates(turn.move, attacker);
                var reachable = candidates.Where(t => CanMoveReachTarget(turn.move, t, out _)).ToList();

                if (reachable.Count == 0)
                {
                    foreach (var candidate in candidates)
                    {
                        _dialogueHandler.DisplayBattleInfo(candidate.Pokemon.pokemonDisplayName +
                                                           candidate.semiInvulnerabilityData.displayMessage);
                    }
                    OnAttackAttempted?.Invoke(false);
                    yield break;
                }

                //accuracy is rolled against a target the move can really hit
                accuracyTarget = reachable[0];
                isSureHit = reachable.Any(t =>
                {
                    CanMoveReachTarget(turn.move, t, out var targetSureHit);
                    return targetSureHit;
                });
                //unreachable targets are reported per target in ApplyMultiTargetDamage
            }
            else if (victim.isSemiInvulnerable)
            {
                if (turn.move.isSelfTargeted)
                {
                    OnAttackAttempted?.Invoke(true);
                    yield break;
                }
                if (victim.semiInvulnerabilityData.IsInvulnerableTo(turn.move, out isSureHit))
                {
                    _dialogueHandler.DisplayBattleInfo(victim.Pokemon.pokemonDisplayName +
                                                       victim.semiInvulnerabilityData.displayMessage);
                    OnAttackAttempted?.Invoke(false);
                    yield break;
                }
            }

            if (!isSureHit)
            {
                if (!MoveSuccessful(turn.move, attacker, accuracyTarget))
                {
                    if (attacker.Pokemon.accuracy >= accuracyTarget.Pokemon.evasion)
                        _dialogueHandler.DisplayBattleInfo(attacker.Pokemon.pokemonDisplayName +
                                                           " missed the attack");
                    else
                        _dialogueHandler.DisplayBattleInfo(accuracyTarget.Pokemon.pokemonDisplayName +
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
                _dialogueHandler.DisplayBattleInfo(attacker.Pokemon.pokemonDisplayName + " flinched!");
            else if (attacker.Pokemon.statusEffect != StatusEffect.None)
            {
                _dialogueHandler.DisplayBattleInfo(attacker.Pokemon.pokemonDisplayName + " is affected by " + attacker.Pokemon.statusEffect);
                yield return _battleVisualsHandler.DisplayStatusEffectVisuals(attacker);
            }
        }
        OnAttackAttempted?.Invoke(false);
    }
    public List<BattleParticipant> GetMultiTargetCandidates(Move move, BattleParticipant attacker)
    {
        var info = move.GetDynamicModule<MultiTargetDamageInfo>();

        if (info != null && info.target == Target.AllExceptSelf)
        {
            return _battleHandler.GetParticipants
                .Where(p => p.isActive && p.participantKey != attacker.participantKey)
                .ToList();
        }

        //AllEnemies, or a pipeline multi-target move (Growl etc.) that has no module
        return attacker.currentEnemies
            .Where(p => p.isActive)
            .ToList();
    }
    private IEnumerator ExecuteMoves()
    {
        //Set priority (switch turns treated as priority 0), speed as tiebreaker
        var priorityList = _turnHistory
            .OrderByDescending(turn => turn.move?.priority ?? 0)
            .ThenByDescending(turn =>
                _battleHandler.GetParticipant(turn.attackerKey).Pokemon.speed)
            .ToList();

        ClearTurn();
        _turnHistory.AddRange(priorityList);

        //handle all swaps turns
        var switchTurns = new List<int>();

        for (var i = 0; i < _turnHistory.Count; i++)
        {
            if (_turnHistory[i].turnUsage == TurnUsage.SwitchOut)
            {
                switchTurns.Add(i);
                yield return HandleSwap(_turnHistory[i].switchData);
                yield return _battleHandler.AwaitFaintQueue();
                yield return _dialogueHandler.AwaitAllDialogue();
            }
        }
        //remove swap turns from history
        if (switchTurns.Count > 0)
        {
            //use ordering to prevent [index out of range] when removing turns
            var orderTurns = switchTurns.OrderByDescending(itemIndex => itemIndex).ToList();
            orderTurns.ForEach(RemoveTurn);
        }

        //handle all attack turns (snapshot so nothing mutating the history can break the loop)
        foreach (var currentTurn in _turnHistory.ToList())
        {
            if (_battleHandler.BattleOver) break;

            if (currentTurn.isCancelled) continue;

            currentTurn.turnExecuted = true;

            var attacker = _battleHandler.GetParticipant(currentTurn.attackerKey);
            var victim = _battleHandler.GetParticipant(currentTurn.victimKey);

            //check on participants
            if (!IsValidParticipantState(attacker))
            {
                //attacker fainted / left: nothing may linger from a half-finished semi move
                AbortSemiInvulnerableState(attacker);
                continue;
            }

            //slot now holds a different pokemon (forced swap). The swap already reset the slot,
            //so there is nothing of ours to clear here
            if (!IsValidParticipant(currentTurn, attacker)) continue;

            //this attacker's own turn is executing, so it stops being semi-invulnerable now
            attacker.isSemiInvulnerable = false;

            yield return attacker.heldItemHandler.CheckForConsumableItem();
            yield return victim.heldItemHandler.CheckForConsumableItem();

            //MULTI-TARGET FIX: the stored victim is only an arbitrary pick for these moves.
            //If it is gone, retarget to any real target that is still standing
            if (currentTurn.move.isMultiTarget && !IsValidParticipantState(victim))
            {
                var replacement = GetMultiTargetCandidates(currentTurn.move, attacker)
                    .FirstOrDefault(IsValidParticipantState);
                if (replacement != null)
                {
                    victim = replacement;
                    currentTurn.victimKey = replacement.participantKey;
                }
            }

            if (!IsValidParticipantState(victim))
            {
                //target is gone: the attack fizzles, and a pending semi-invulnerable
                //hit turn must be cancelled or the next Fly would skip its charge turn
                AbortSemiInvulnerableState(attacker);
                if (attacker.previousMoveData != null)
                    attacker.previousMoveData.failedAttempt = true;

                _dialogueHandler.DisplayBattleInfo("But there was no target...");
                yield return _dialogueHandler.AwaitAllDialogue();
                continue;
            }

            foreach (var handler in _moveExecutionHandlers)
            {
                //handle all event subscribers for [Move Execution]
                IEnumerator routine = handler(attacker);
                if (routine is not null)
                {
                    yield return StartCoroutine(routine);
                }
            }

            yield return _dialogueHandler.AwaitAllDialogue();

            if (currentTurn.turnUsage == TurnUsage.UseStruggle)
            {
                _dialogueHandler.DisplayBattleInfo(attacker.Pokemon.pokemonDisplayName + " is out of moves");
                yield return _dialogueHandler.AwaitAllDialogue();
                _dialogueHandler.DisplayBattleInfo(attacker.Pokemon.pokemonDisplayName + " used struggle");

                //struggle cannot reach a Fly / Dig / Dive user
                if (victim.isSemiInvulnerable)
                {
                    _dialogueHandler.DisplayBattleInfo(victim.Pokemon.pokemonDisplayName +
                                                       victim.semiInvulnerabilityData.displayMessage);
                    yield return _dialogueHandler.AwaitAllDialogue();
                    continue;
                }

                yield return _moveUsageHandler.DealStruggleDamage(victim, attacker, currentTurn.move);
                yield return _battleHandler.AwaitFaintQueue();
                yield return _dialogueHandler.AwaitAllDialogue();
                continue;
            }

            bool successfulAttack = false;
            OnAttackAttempted += GetAttackResult;
            void GetAttackResult(bool result)
            {
                OnAttackAttempted -= GetAttackResult;
                successfulAttack = result;
            }

            yield return CheckAttackSuccess(currentTurn, attacker, victim);
            yield return _dialogueHandler.AwaitAllDialogue();

            yield return _battleHandler.AwaitFaintQueue();
            CheckRepeatedMove(attacker, currentTurn.move);
            if (successfulAttack)
            {
                _moveUsageHandler.BeginMoveExecution(currentTurn);

                yield return _moveUsageHandler.AwaitMoveCompletion();
                yield return _dialogueHandler.AwaitAllDialogue();
                yield return _battleHandler.AwaitFaintQueue();
                yield return _dialogueHandler.AwaitAllDialogue();
                yield return attacker.heldItemHandler.CheckForConsumableItem();
                yield return victim.heldItemHandler.CheckForConsumableItem();
            }
            else
            {
                if (attacker.previousMoveData != null)
                    attacker.previousMoveData.failedAttempt = true;
                AbortSemiInvulnerableState(attacker);
            }
        }

        yield return _battleHandler.AwaitFaintQueue();
        yield return _dialogueHandler.AwaitAllDialogue();

        ClearTurn();
        OnTurnsCompleted?.Invoke();

        if (_battleHandler.BattleOver) yield break;

        //check held item and status effect
        var statusCheckList = _battleHandler.GetValidParticipants();
        foreach (var participant in statusCheckList)
        {
            yield return participant.heldItemHandler.CheckForConsumableItem();
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

        //anything that fainted (and was not replaced) during this round must not keep
        //semi-invulnerable / cooldown state, or it will pre-add a turn for a dead slot
        foreach (var participant in _battleHandler.GetParticipants)
        {
            if (IsValidParticipantState(participant)) continue;

            //keep the original behaviour: dead / deactivated slots still get their moves reloaded
            if (participant.Pokemon != null)
                participant.pokemonState.ResetMoveData();

            AbortSemiInvulnerableState(participant);
            if (participant.currentCoolDown is {isCoolingDown:true})
                participant.currentCoolDown.ResetState();
        }

        //semi-invulnerability turn logic and cooldown check
        //(fresh list: the one used for status checks is stale after faints / replacements)
        foreach (var participant in _battleHandler.GetValidParticipants())
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
                    }
                }
            }

            participant.pokemonState.ResetMoveData();

            if (!participant.isSemiInvulnerable) continue;

            //flag without data would pre-add a null turn, so clear the flag instead
            if (participant.semiInvulnerabilityData.turnData == null)
            {
                participant.isSemiInvulnerable = false;
                continue;
            }

            AddTurn(new Turn(participant.semiInvulnerabilityData.turnData));
        }
        OnTurnEventsCompleted?.Invoke();
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
        if (_pokemonPartyHandler.GetLivingPokemonCount() < 2)
        {
            //Don't have a valid switch
            yield break;
        }
        yield return _dialogueHandler.AwaitAllDialogue();
        
        _dialogueHandler.DisplayCustomOptions($"{trainerName} is about to use {pokemonName}, change pokemon?",
            new[] { "Yes", "No" }, new Action[] { SwitchAccepted, SwitchRejected });
        
        bool processing = true;
        yield return new WaitUntil(() => !processing);
        yield break;
        void SwitchRejected()
        {
            processing = false;
        }
        void SwitchAccepted()
        {
            _battleHandler.OnSwitchIn += ResetEvent;
            _inputStateHandler.OnStateRemoved += SwitchRejectedInUI;
            _dialogueHandler.EndDialogue();
            _battleHandler.GetParticipant(BattleParticipantKey.Player).SetupSwitchOut(PartyUsage.SwitchOutFromBattleStyle);
        }
        void ResetEvent()
        {
            _inputStateHandler.OnStateRemoved -= SwitchRejectedInUI;
            _battleHandler.OnSwitchIn -= ResetEvent;
            processing = false;
        }
        void SwitchRejectedInUI(InputState previousState)
        {
            if (previousState.stateName != InputStateName.PokemonPartyNavigation) return;
            _inputStateHandler.OnStateRemoved -= SwitchRejectedInUI;
            _battleHandler.OnSwitchIn -= ResetEvent;
            processing = false;
        }
    }
    public IEnumerator HandleSwap(SwitchOutData swap, bool forcedSwap=false)
    {
        if (forcedSwap)
        {
            _dialogueHandler.DisplayBattleInfo(swap.participant.Pokemon.pokemonDisplayName
                                                        + " was blown out");
        }
        else
        {
            if (swap.participant.isPlayer)
            {
                _dialogueHandler.DisplayBattleInfo(_gameLoadingHandler.playerData.playerName
                                                            + " withdrew " + swap.participant.Pokemon.pokemonDisplayName);
            }
            else
            {
                _dialogueHandler.DisplayBattleInfo(swap.participant.pokemonTrainerAI.trainerData.trainerName
                                                            +" withdrew "+swap.participant.Pokemon.pokemonDisplayName);
            }
        }
        yield return _dialogueHandler.AwaitAllDialogue();
        
        foreach (var turn in _turnHistory)
        { 
            //check if the move used was pursuit
            if (turn.move is null) continue; 
            if (!NameDB.NameMatch(turn.move, MoveName.Pursuit)) continue;
           
            var victim= _battleHandler.GetParticipant(turn.victimKey);
            if (victim != swap.participant) continue;
            
            var attacker= _battleHandler.GetParticipant(turn.attackerKey);
            turn.isCancelled = true;//since it strikes here

            //pursuit damage
            var pursuit = turn.move;
            _dialogueHandler.DisplayBattleInfo($"{attacker.Pokemon.pokemonDisplayName} used Pursuit on {victim.Pokemon.pokemonDisplayName}!");

            var pursuitDamage = _moveUsageHandler.CalculateMoveDamage(pursuit,attacker, victim) * 2;
            _moveUsageHandler.DisplaySpecialDamage(victim,predefinedDamage:pursuitDamage);
            yield return _moveUsageHandler.AwaitDamageDisplay();
            if (victim.Pokemon.hp <= 0)
            {
                yield break;
            }
        }
        
        yield return _dialogueHandler.AwaitAllDialogue();
        
        //Complete the swap
        if (!forcedSwap)SoundManager.Play(JingleId.WithdrawPokemon);
        
        if (swap.participant.isPlayer || forcedSwap)
        {
            yield return _battleVisualsHandler.WithdrawPokemon(swap.participant);
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
            swap.participant.pokemonTrainerAI.SwapIndexes(swap.partyPosition, swap.memberToSwapWith);
            var enemyParty = swap.participant.pokemonTrainerAI.TrainerParty;
            yield return _battleIntroHandler.SwitchInPokemon(swap.participant,enemyParty[swap.partyPosition],!forcedSwap);
        }
    }

    public bool ContainsSwitch(int memberPosition,bool isPlayerSwitch)
    {
        foreach (var turn in _turnHistory)
        {
            if (turn.turnUsage==TurnUsage.SwitchOut)//must check this first to avoid null ref
            {
                var hasPlayerKey = turn.switchData.participant.participantKey
                    is BattleParticipantKey.Player
                    or BattleParticipantKey.PlayerPartner;
                
                var isCorrectParticipant = isPlayerSwitch ? hasPlayerKey : !hasPlayerKey;
                if(isCorrectParticipant)
                {
                    if (turn.switchData.memberToSwapWith == memberPosition)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    public string GetMoveUsageText(Move move, BattleParticipant attacker,BattleParticipant victim)
    {
        if (move.displayTargetMessage)
            return attacker.Pokemon.pokemonDisplayName + " used " 
                                                + move.moveName + " on " + victim.Pokemon.pokemonDisplayName + "!";
        return attacker.Pokemon.pokemonDisplayName + " used " + move.moveName + "!";
    }
   
    private void ResolveTurnStateForParticipants()
    {
        if (_battleHandler.BattleOver) return;
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (!currentParticipant.isActive) return;

        StartCoroutine(DetermineTurnHandling());
        return;

        IEnumerator DetermineTurnHandling()
        {
            var coolDown = currentParticipant.currentCoolDown;
            if (coolDown.isCoolingDown)
            {
                if (coolDown.isExecutionTurn)
                {
                    SaveTurn(new(coolDown.turnData));
                    yield break;
                }

                if (coolDown.canDisplayMessage)
                {
                    _dialogueHandler.DisplayBattleInfo(currentParticipant.Pokemon.pokemonDisplayName
                                                       + coolDown.coolDownMessage);
                    yield return _dialogueHandler.AwaitAllDialogue();
                    if (_battleHandler.BattleOver) yield break;
                }
                coolDown.numTurns--;
                AdvanceAfterAutomaticTurn();
                yield break;
            }

            if (currentParticipant.isSemiInvulnerable)
            {
                //their turn was pre-added at the end of the last round, no input needed
                AdvanceAfterAutomaticTurn();
                yield break;
            }

            if (currentParticipant.isPlayer)
            {
                //allow player input
                _inputStateHandler.ResetRelevantUi(new[]
                {
                    InputStateName.PokemonBattleEnemySelection, InputStateName.PlaceHolder
                });
            }
            else
            {
                //allow enemy AI decision
                if (_battleHandler.isTrainerBattle)
                {
                    currentParticipant.pokemonTrainerAI.MakeBattleDecision();
                }
                else
                {
                    _wildPokemonAI.MakeBattleDecision();
                }
            }
        }
    }
    private void CheckRepeatedMove(BattleParticipant attacker, Move move)
    {
        if (attacker.previousMoveData is null)
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
        return participant.Pokemon is { hp: > 0 };
    }

    private bool IsValidParticipant(Turn turn,BattleParticipant participant)
    {
        return turn.attackerID == participant.Pokemon.pokemonID||
                turn.victimID == participant.Pokemon.pokemonID;
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
        var step = _battleHandler.isDoubleBattle ? 1 : 2;
        var previousKey = (BattleParticipantKey)(currentTurnIndex - step);

        var historyIndex = _turnHistory.FindIndex(t => t.attackerKey == previousKey);
        if (historyIndex >= 0)
            RemoveTurn(historyIndex);

        currentTurnIndex -= step;
        _inputStateHandler.OnStateRemoved -= _battleHandler.SetupOptionsAfterTurnReset;
        _inputStateHandler.OnStateRemoved += _battleHandler.SetupOptionsAfterTurnReset;
    }
    
    private void ChangeTurn(int maxParticipantIndex,int step)
    {
        if (currentTurnIndex < maxParticipantIndex)
            currentTurnIndex+=step;
        else
            currentTurnIndex = 0;
        
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (!currentParticipant.isActive && !currentParticipant.awaitingRevival)
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
    private bool MoveSuccessful(Move move, BattleParticipant attacker, BattleParticipant target)
    {
        var random = Utility.RandomRange100();
        var hitChance = move.moveAccuracy *
                        (attacker.Pokemon.accuracy / target.Pokemon.evasion);
        return hitChance > random;
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
            _moveUsageHandler.InitiateStatChange(spDefBuff);
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
                if (participant.Pokemon.HasType(protectedType))
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
                            _moveUsageHandler.InitiateStatChange(spDefBuff);
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
            if (participant.Pokemon.HasType(PokemonType.Ice)) continue;
            yield return DealWeatherDamage(participant);
        }
    }

    private void DamageModifierForWeather(PokemonType type,float damageFactor,DamageModifierSource source)
    { 
        var damageModifierInfo = new DamageModifierInfo();
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
        _dialogueHandler.DisplayBattleInfo(victim.Pokemon.pokemonDisplayName + CurrentWeather.weatherDamageMessage);
        var weatherDamage = victim.Pokemon.maxHp * (1 / 16f);
        _moveUsageHandler.DisplaySpecialDamage(victim, predefinedDamage:weatherDamage);
        yield return _moveUsageHandler.AwaitDamageDisplay();
    }
}
