using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class MoveLogicDatabase : MonoBehaviour,IInjectable
{
    private DialogueHandler _dialogueHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    private BattleVisuals _battleVisualsHandler;
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
    private BattleOperations _battleOperationsHandler;
    private MoveLogicHandler _moveLogicHandler;
    
    private Dictionary<MoveName, Func<Turn,BattleParticipant,BattleParticipant,IEnumerator>> _logicMethods = new();
    
    public void Inject(ServiceContainer container)
    {
        _battleOperationsHandler = container.Resolve<BattleOperations>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        _moveLogicHandler = container.Resolve<MoveLogicHandler>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _logicMethods.Add(MoveName.BrickBreak, BrickBreak);
        _logicMethods.Add(MoveName.Haze, Haze);
        _logicMethods.Add(MoveName.TakeDown, TakeDown);
        _logicMethods.Add(MoveName.Magnitude, Magnitude);
        _logicMethods.Add(MoveName.FuryCutter, FuryCutter);
        _logicMethods.Add(MoveName.Flail, Flail);
        _logicMethods.Add(MoveName.FalseSwipe, FalseSwipe);
        _logicMethods.Add(MoveName.BellyDrum, BellyDrum);
        _logicMethods.Add(MoveName.Covet, Covet);
        _logicMethods.Add(MoveName.Rest, Rest);
        _logicMethods.Add(MoveName.Endeavor, Endeavor);
        _logicMethods.Add(MoveName.Thunder, Thunder);
        _logicMethods.Add(MoveName.Whirlwind, Whirlwind);
        _logicMethods.Add(MoveName.Silverwind, SilverWind);
        _logicMethods.Add(MoveName.MirrorMove, MirrorMove);
        
        //still need tests
        _logicMethods.Add(MoveName.HyperBeam, Hyperbeam);
        _logicMethods.Add(MoveName.Bide, Bide);
    }
    
    public IEnumerator InvokeMoveLogic(BattleParticipant attacker, BattleParticipant victim, Turn currentTurn)
    {
        var moveNameEnum = NameDB.ParseMoveName(currentTurn.move.moveName);
        
        if (_logicMethods.TryGetValue(moveNameEnum, out var logicMethod))
        {
            yield return logicMethod(currentTurn,attacker,victim); 
        }
        else
            Debug.LogWarning($"Move '{moveNameEnum}' not found!");
    }
    private IEnumerator BrickBreak(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        var duplicateBarriers = new List<string>();
        foreach (var enemy in attacker.currentEnemies)
        {
            if(!enemy.isActive)continue;
            foreach (var barrier in enemy.barriers)
            {
                if (duplicateBarriers.Contains(barrier.barrierName))
                {
                    //participants share barriers, so only display the message the first time 
                    //and not again when partner's barrier is broken
                    continue;
                }
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" shattered "+barrier.barrierName);
                duplicateBarriers.Add(barrier.barrierName);
            }
            enemy.barriers.Clear();
        }
        
        yield return _dialogueHandler.AwaitAllDialogue();
        _moveUsageHandler.DisplayMoveDamage(currentTurn.move,attacker,victim);
        yield return _moveUsageHandler.AwaitDamageDisplay();
    }
    
    private IEnumerator Haze(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        var validParticipants = _battleHandler.GetValidParticipants();
        foreach (var participant in validParticipants)
        {
            participant.pokemon.statModifiers.Clear();
            participant.statData.LoadActualStats(true);
        }
        yield return null;
    }

    private IEnumerator Hyperbeam(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        _moveUsageHandler.DisplayMoveDamage(currentTurn.move,attacker,victim);
        var cancelledTurn = new Turn(currentTurn);
        cancelledTurn.isCancelled = true;
        attacker.currentCoolDown.UpdateCoolDown( 1,cancelledTurn,message: " must recharge!");
        yield return null;
    }

    private IEnumerator Bide(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        if (attacker.currentCoolDown.isExecutionTurn)
        {
            _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" unleashed the power");
            if (attacker.currentCoolDown.turnData.move.moveDamage > 0)
            {
                currentTurn.move.moveDamage = attacker.currentCoolDown.turnData.move.moveDamage;
                var typelessDamage = _moveUsageHandler.CalculateMoveDamage(currentTurn.move,attacker, victim, true);
                _moveUsageHandler.DisplaySpecialDamage(victim, predefinedDamage: typelessDamage);
            }
            else
            {
                _dialogueHandler.DisplayBattleInfo("But it failed!");
            }
           
            _moveUsageHandler.OnMoveHit -= attacker.currentCoolDown.StoreDamage;
            attacker.currentCoolDown.ResetState();
            yield return null;
        }
        else
        {
            attacker.currentCoolDown.UpdateCoolDown(2,currentTurn, " is storing power");//change turns back
            _moveUsageHandler.OnMoveHit += attacker.currentCoolDown.StoreDamage;
        }
       
    }
    
    private IEnumerator TakeDown(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        var damage = _moveUsageHandler.CalculateMoveDamage(currentTurn.move,attacker, victim);
        var recoilDamage = math.floor(damage / 4f);
        
        _moveUsageHandler.DisplaySpecificMoveDamage(currentTurn.move,victim,damage);
        yield return _moveUsageHandler.AwaitDamageDisplay();
        
        _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName +" was hurt by the recoil");
        _moveUsageHandler.DisplaySpecialDamage(attacker,predefinedDamage:recoilDamage);
        yield return _moveUsageHandler.AwaitDamageDisplay();
    }
    
    public float MagnitudeDamageEffect(int predefinedStrength = 0)
    {
        int magnitudeStrength = predefinedStrength != 0? predefinedStrength
                    : Utility.RandomRange(4, 11);
        
        var baseDamage = 10f;
        var damageIncrease = 0f;
        if(magnitudeStrength > 4)
        {
            damageIncrease = 20f;
        }
        baseDamage += damageIncrease * (magnitudeStrength - 4);
        if (magnitudeStrength == 10)
        {
            baseDamage += 20f;
        }
        return baseDamage;
    }
    private IEnumerator Magnitude(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        int magnitudeStrength = Utility.RandomRange(4, 11);

        var baseDamage = MagnitudeDamageEffect(magnitudeStrength);
        
        _dialogueHandler.DisplayBattleInfo("Magnitude level "+magnitudeStrength);
        currentTurn.move.moveDamage = baseDamage;
        
        yield return _moveLogicHandler.ApplyMultiTargetDamage(
            _moveLogicHandler.TargetAllExceptSelf(attacker)
            ,currentTurn.move,attacker);
    }

    private IEnumerator Endeavor(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        if (victim.pokemon.hp < attacker.pokemon.hp)
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            yield break;
        }
        var damage = victim.pokemon.hp - attacker.pokemon.hp;
        _moveUsageHandler.DisplaySpecificMoveDamage(currentTurn.move,victim,damage);
    }

    private IEnumerator FuryCutter(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        var damageLevel = new[] { 10f, 20f, 40f, 80f, 160f };
        if (attacker.previousMoveData.move.moveName == NameDB.GetMoveName(MoveName.FuryCutter))
        {
            currentTurn.move.moveDamage = attacker.previousMoveData.numRepetitions > 3?
                damageLevel[^1] : damageLevel[attacker.previousMoveData.numRepetitions];
        }
        else
            currentTurn.move.moveDamage = damageLevel[0];
        
        _moveUsageHandler.DisplayMoveDamage(currentTurn.move,attacker,victim);
        yield return null;
    }
    private IEnumerator SilverWind(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        bool battleEnded = false;
        bool awaitingFaint = true;
        
        var damage = _moveUsageHandler.CalculateMoveDamage(currentTurn.move,attacker, victim);
      
        if (victim.pokemon.hp - damage <= 0)
        {
            _battleHandler.OnFaintSequenceComplete += CancelOnBattleEnd;
            void CancelOnBattleEnd(BattleParticipant faintedParticipant)
            {
                if (faintedParticipant.participantKey != victim.participantKey) return;
                _battleHandler.OnFaintSequenceComplete -= CancelOnBattleEnd;
                battleEnded = _battleHandler.BattleOver;
                awaitingFaint = false;
            }
        }else awaitingFaint = false;
        
        _moveUsageHandler.DisplaySpecificMoveDamage(currentTurn.move,victim,damage);
        yield return _moveUsageHandler.AwaitDamageDisplay();
        
        yield return new WaitUntil(() => !awaitingFaint);
        
        if(battleEnded) yield break;
        
        if (Utility.RandomRange100() > currentTurn.move.statusChance)
        {
            yield break;
        }
        //get buffs
        var stats = new[]
        {
            Stat.Attack, Stat.Defense, 
            Stat.SpecialAttack, Stat.SpecialDefense,
            Stat.Speed
        };
        
        foreach (var statToBuff in stats)
        {
            bool awaitingAddition = true;
            _battleOperationsHandler.OnStatChangeApplied += AwaitBuffAddition;
            var buffData = new StatChangeTransitData(attacker, statToBuff, true, 1);
            _moveUsageHandler.InitiateStatChange(buffData,false);
            yield return new WaitUntil(() => !awaitingAddition);
            continue;
            void AwaitBuffAddition(StatChangeOperationData operationData)
            {
                _battleOperationsHandler.OnStatChangeApplied -= AwaitBuffAddition;
                awaitingAddition = false;
            }
        }
        
        string statChangeMessage = _battleOperationsHandler.GetStatModResultMessage(true,attacker.pokemon,stats);
        _battleVisualsHandler.OnStatVisualDisplayed += AwaitBuffVisual;
        bool awaitingDisplay = true;
        _battleVisualsHandler.SelectStatChangeVisuals(Stat.Multi,attacker,statChangeMessage);
        yield return new WaitUntil(() => !awaitingDisplay);
        yield break;
        void AwaitBuffVisual()
        {
            _battleVisualsHandler.OnStatVisualDisplayed -= AwaitBuffVisual;
            awaitingDisplay = false;
        }
    }

    private IEnumerator Flail(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        List<(int hpLevel, float damage)> damagePerLevel = new()
        {
            (32, 200f), (16, 150f), (8, 100f), (4, 80f), (2, 40f)
        };

        var currentHpRatio = attacker.pokemon.hp / attacker.pokemon.maxHp;

        foreach (var phase in damagePerLevel)
        {
            if (currentHpRatio <= 1f / phase.hpLevel)
            {
                currentTurn.move.moveDamage = phase.damage;
                break;
            }
        }
        _moveUsageHandler.DisplayMoveDamage(currentTurn.move,attacker,victim);
        yield return null;
    }

    private IEnumerator FalseSwipe(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        var damage = _moveUsageHandler.CalculateMoveDamage(currentTurn.move,attacker, victim);
        damage = Mathf.Min(damage, victim.pokemon.hp - 1);
        damage = Mathf.Max(damage, 0);
        _moveUsageHandler.DisplaySpecificMoveDamage(currentTurn.move,victim,damage);
        yield return null;
    }

    private IEnumerator BellyDrum(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        if (attacker.pokemon.hp < 2)
        {
            _dialogueHandler.DisplayBattleInfo("But it failed!");
            yield break;
        }
        
        var selfDamage = Mathf.FloorToInt(attacker.pokemon.maxHp / 2f);
        _moveUsageHandler.DisplaySpecialDamage(attacker,selfDamage);
        
        var buffData = new StatChangeTransitData(attacker, Stat.Attack, true, 6);
        _moveUsageHandler.InitiateStatChange(buffData);
    }

    private IEnumerator Covet(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        _moveUsageHandler.DisplayMoveDamage(currentTurn.move,attacker,victim);
        if (victim.pokemon.hasItem && !attacker.pokemon.hasItem)
        {
            if (victim.pokemon.heldItem.itemType == ItemType.Berry)
            {
                attacker.pokemon.GiveItem(InstanceFactory.CreateItem(victim.pokemon.heldItem));
                victim.pokemon.RemoveHeldItem();
            }
        }
        yield return null;
    }

    private IEnumerator MirrorMove(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        if (victim.previousMoveData is {failedAttempt:false})
        {
            var nonCopyableMoves = new[] {MoveName.Detect,MoveName.Protect,MoveName.Haze};
            
            var invalidMove = nonCopyableMoves.Contains(NameDB.ParseMoveName(victim.previousMoveData.move.moveName));
            
            if (victim.previousMoveData.move.isSelfTargeted || invalidMove)
            {
                _dialogueHandler.DisplayBattleInfo("But it failed!");
                yield break;
            }
            _moveUsageHandler.AllowMoveRepeat();
            currentTurn.move = victim.previousMoveData.move;
            _dialogueHandler.DisplayBattleInfo(
                _turnBasedCombatHandler.GetMoveUsageText(currentTurn.move,attacker, victim));
            _moveUsageHandler.OnMoveComplete += ()=> _moveUsageHandler.BeginMoveExecution(currentTurn);
        }
        else
        {
            _dialogueHandler.DisplayBattleInfo("But it failed!");
        }
    }

    private IEnumerator Whirlwind(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        if (_battleHandler.isDoubleBattle && victim.GetPartner().participantKey == attacker.participantKey)
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            yield break;
        }
        if (attacker.pokemon.currentLevel < victim.pokemon.currentLevel)
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            yield break;
        }
        if (!_battleHandler.isTrainerBattle)
        {
            _battleHandler.EndBattle(BattleEndState.BattleTerminated);
            _moveUsageHandler.ResetAfterBattleTermination();
            yield break;
        }

        int partyPositionOfVictim;
        int partyPositionOfPartner;
        
        if (victim.participantKey < victim.GetPartnerKey())
        {
            partyPositionOfVictim = 0;
            partyPositionOfPartner = 1;
        }
        else
        {
            partyPositionOfVictim = 1;
            partyPositionOfPartner = 0;
        }
        
        if (victim.isPlayer)
        {
            yield return CreateSwitchData(_pokemonPartyHandler.GetLivingPokemonIndexes());
        }
        else
        {
            var enemyTrainer = victim.pokemonTrainerAI;
            yield return CreateSwitchData(enemyTrainer.GetLivingPokemonIndexes());
        }
        yield break;
        IEnumerator CreateSwitchData(List<int> living)
        {
            //exclude current participants
            var available = living
                .Where(index => index != partyPositionOfVictim)
                .ToList();
            if (_battleHandler.isDoubleBattle)
            {
                // Also exclude the other active Pokémon's party position
                available.RemoveAll(index => index == partyPositionOfPartner);
            }

            if (available.Count == 0)
            {
                _dialogueHandler.DisplayBattleInfo("but it failed!");
                yield break;
            }
            
            var pokemonIndex = available[Utility.RandomRange(0, available.Count)];
            
            var switchData = new SwitchOutData(partyPositionOfVictim,pokemonIndex,victim);

            yield return _turnBasedCombatHandler.HandleSwap(switchData,true);
        }
    }
    private IEnumerator Rest(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {
        var healthGain = attacker.pokemon.maxHp - attacker.pokemon.hp;
        if (healthGain <= 0)
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            yield break;
        }
        _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" fell asleep!");
        yield return new WaitForSeconds(0.25f);
        _moveUsageHandler.HealthGainDisplay(healthGain,healthGainer:attacker);
        attacker.statusHandler.RemoveStatusEffect(true);
        yield return _moveUsageHandler.AwaitHealthGainDisplay();
        _moveUsageHandler.ApplyStatusToVictim(attacker, StatusEffect.Sleep, 2);
        yield return _dialogueHandler.AwaitAllDialogue();
    }

    private IEnumerator Thunder(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
    {        
        if (!victim.canBeDamaged)
        { 
            _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName+" protected itself");
            yield break;
        }
        var currentWeather = _turnBasedCombatHandler.CurrentWeather;
        if (currentWeather.weather == Weather.Rain)
        {
            currentTurn.move.isSureHit = true;
        }
        if (currentWeather.weather == Weather.Sunlight)
        {
            currentTurn.move.moveAccuracy = 50f;
        }
        _moveUsageHandler.DisplayMoveDamage(currentTurn.move,attacker,victim);
        yield return _moveUsageHandler.AwaitDamageDisplay();
        
        if (Utility.RandomRange100() < currentTurn.move.statusChance)
        {
            _moveUsageHandler.HandleStatusApplication(victim,currentTurn.move,true);
        }
        yield return null;
    }
}
