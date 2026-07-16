using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class MoveLogicDatabase : MonoBehaviour,IInjectable
{
    private Dialogue_handler _dialogueHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Pokemon_party _pokemonPartyHandler;
    private BattleVisuals _battleVisualsHandler;
    private Battle_handler _battleHandler;
    private Move_handler _moveUsageHandler;
    private BattleOperations _battleOperationsHandler;
    private MoveLogicHandler _moveLogicHandler;
    
    private Dictionary<string, Func<Turn,Battle_Participant,Battle_Participant,IEnumerator>> _logicMethods = new();
    
    public void Inject(ServiceContainer container)
    {
        _battleOperationsHandler = container.Resolve<BattleOperations>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _moveUsageHandler = container.Resolve<Move_handler>();
        _moveLogicHandler = container.Resolve<MoveLogicHandler>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _logicMethods.Add("brickbreak", BrickBreak);
        _logicMethods.Add("haze", Haze);
        _logicMethods.Add("hyperbeam", Hyperbeam);
        _logicMethods.Add("bide", Bide);
        _logicMethods.Add("sonicboom", SonicBoom);
        _logicMethods.Add("takedown", TakeDown);
        _logicMethods.Add("magnitude", Magnitude);
        _logicMethods.Add("endeavor", Endeavor);
        _logicMethods.Add("furycutter", FuryCutter);
        _logicMethods.Add("silverwind", Silverwind);
        _logicMethods.Add("flail", Flail);
        _logicMethods.Add("falseswipe", FalseSwipe);
        _logicMethods.Add("bellydrum", BellyDrum);
        _logicMethods.Add("covet", Covet);
        _logicMethods.Add("mirrormove", MirrorMove);
        _logicMethods.Add("whirlwind", Whirlwind);
        _logicMethods.Add("rest", Rest);
    }
    
    public IEnumerator InvokeMoveLogic(Battle_Participant attacker, Battle_Participant victim, Turn currentTurn)
    {
        var formattedName = currentTurn.move.moveName.Replace(" ", "").ToLower();
        
        if (_logicMethods.TryGetValue(formattedName, out var logicMethod))
        {
            yield return logicMethod(currentTurn,attacker,victim); 
        }
        else
            Debug.LogWarning($"Move '{formattedName}' not found!");
    }
    private IEnumerator BrickBreak(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        var duplicateBarriers = new List<string>();
        foreach (var enemy in attacker.currentEnemies)
        {
            if(!enemy.isActive)continue;
            foreach (var barrier in enemy.barriers)
            {
                if (duplicateBarriers.Contains(barrier.barrierName)) continue;
                _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" shattered "+barrier.barrierName);
                duplicateBarriers.Add(barrier.barrierName);
            }
            enemy.barriers.Clear();
        }
        
        yield return _dialogueHandler.AwaitAllDialogue();
        _moveUsageHandler.DisplayMoveDamage(currentTurn.move,attacker,victim);
        yield return _moveUsageHandler.AwaitDamageDisplay();
    }
    
    private IEnumerator Haze(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        var validParticipants = _battleHandler.GetValidParticipants();
        foreach (var participant in validParticipants)
        {
            participant.pokemon.buffAndDebuffs.Clear();
            participant.statData.LoadActualStats();
        }
        yield return null;
    }

    private IEnumerator Hyperbeam(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        _moveUsageHandler.DisplayMoveDamage(currentTurn.move,attacker,victim);
        var cancelledTurn = new Turn(currentTurn);
        cancelledTurn.isCancelled = true;
        attacker.currentCoolDown.UpdateCoolDown( 1,cancelledTurn,message: " must recharge!");
        yield return null;
    }

    private IEnumerator Bide(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
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
           
            _moveUsageHandler.OnDamageDeal -= attacker.currentCoolDown.StoreDamage;
            attacker.currentCoolDown.ResetState();
            yield return null;
        }
        else
        {
            attacker.currentCoolDown.UpdateCoolDown(2,currentTurn, " is storing power");//change turns back
            _moveUsageHandler.OnDamageDeal += attacker.currentCoolDown.StoreDamage;
        }
       
    }

    private IEnumerator SonicBoom(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        var sonicBoomDamage = 20f;
        _moveUsageHandler.DisplaySpecificMoveDamage(currentTurn.move,victim,specificDamage:sonicBoomDamage);
        yield return null;
    }    
    private IEnumerator TakeDown(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        var damage = _moveUsageHandler.CalculateMoveDamage(currentTurn.move,attacker, victim);
        var recoilDamage = math.floor(damage / 4f);
        
        _moveUsageHandler.DisplaySpecificMoveDamage(currentTurn.move,victim,damage);
        yield return _moveUsageHandler.AwaitDamageDisplay();
        
        _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName +" was hurt by the recoil");
        _moveUsageHandler.DisplaySpecialDamage(attacker,predefinedDamage:recoilDamage);
        yield return _moveUsageHandler.AwaitDamageDisplay();
    }

    private IEnumerator Magnitude(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        var magnitudeStrength = Utility.RandomRange(4, 11);
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
        _dialogueHandler.DisplayBattleInfo("Magnitude level "+magnitudeStrength);
        currentTurn.move.moveDamage = baseDamage;
        
        yield return _moveLogicHandler.ApplyMultiTargetDamage(
            _moveLogicHandler.TargetAllExceptSelf(attacker)
            ,currentTurn.move,attacker);
    }

    private IEnumerator Endeavor(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        if (victim.pokemon.hp < attacker.pokemon.hp)
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            yield break;
        }
        var damage = victim.pokemon.hp - attacker.pokemon.hp;
        _moveUsageHandler.DisplaySpecificMoveDamage(currentTurn.move,victim,damage);
    }

    private IEnumerator FuryCutter(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        var damageLevel = new[] { 10f, 20f, 40f, 80f, 160f };
        if (attacker.previousMoveData.move.moveName == NameDB.GetMoveName(LearnSetMoveName.FuryCutter))
        {
            currentTurn.move.moveDamage = attacker.previousMoveData.numRepetitions > 4?
                damageLevel[^1] : damageLevel[attacker.previousMoveData.numRepetitions];
        }
        else
            currentTurn.move.moveDamage = damageLevel[0];
        
        _moveUsageHandler.DisplayMoveDamage(currentTurn.move,attacker,victim);
        yield return null;
    }
    private IEnumerator Silverwind(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        bool battleEnded = false;
        _battleHandler.OnParticipantFainted += CancelOnBattleEnd;
        _moveUsageHandler.DisplayMoveDamage(currentTurn.move,attacker,victim);
        yield return _moveUsageHandler.AwaitDamageDisplay();

        void CancelOnBattleEnd(Battle_Participant faintedParticipant)
        {
            if (faintedParticipant != victim) return;
            _battleHandler.OnParticipantFainted -= CancelOnBattleEnd;
            battleEnded = _battleHandler.battleOver;
        }
        
        if(battleEnded) yield break;
        _battleHandler.OnParticipantFainted -= CancelOnBattleEnd;
        
        if (Utility.RandomRange(0, 101) > 10)
        {
            yield return null;
            yield break;
        }
        
        //get buffs
        var allBuffs = new[]
        {
            Stat.Attack, Stat.Defense, 
            Stat.SpecialAttack, Stat.SpecialDefense,
            Stat.Speed
        };
        
        foreach (var buff in allBuffs)
        {
            bool awaitingAddition = true;
            _battleOperationsHandler.OnBuffApplied += AwaitBuffAddition;
            var buffData = new BuffDebuffData(attacker, buff, true, 1);
            _moveUsageHandler.ExecuteBuffOrDebuff(buffData,false);
            yield return new WaitUntil(() => !awaitingAddition);
            void AwaitBuffAddition()
            {
                _battleOperationsHandler.OnBuffApplied -= AwaitBuffAddition;
                awaitingAddition = false;
            }
        }
        
        string statChangeMessage = _battleOperationsHandler.GetBuffResultMessage(true,attacker.pokemon,allBuffs);
        _battleVisualsHandler.OnStatVisualDisplayed += AwaitBuffVisual;
        bool awaitingDisplay = true;
        _battleVisualsHandler.SelectStatChangeVisuals(Stat.Multi,attacker,statChangeMessage);
        yield return new WaitUntil(() => !awaitingDisplay);
        void AwaitBuffVisual()
        {
            _battleVisualsHandler.OnStatVisualDisplayed -= AwaitBuffVisual;
            awaitingDisplay = false;
        }
    }

    private IEnumerator Flail(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
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

    private IEnumerator FalseSwipe(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        var damage = _moveUsageHandler.CalculateMoveDamage(currentTurn.move,attacker, victim);
        damage = Mathf.Min(damage, victim.pokemon.hp - 1);
        damage = Mathf.Max(damage, 0);
        _moveUsageHandler.DisplaySpecificMoveDamage(currentTurn.move,victim,damage);
        yield return null;
    }

    private IEnumerator BellyDrum(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        if (attacker.pokemon.hp < 2)
        {
            _dialogueHandler.DisplayBattleInfo("But it failed!");
            yield break;
        }
        
        var selfDamage = math.floor(attacker.pokemon.hp / 2f);
        _moveUsageHandler.DisplaySpecialDamage(attacker,selfDamage);
        
        var buffData = new BuffDebuffData(attacker, Stat.Attack, true, 6);
        _moveUsageHandler.ExecuteBuffOrDebuff(buffData);
    }

    private IEnumerator Covet(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
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

    private IEnumerator MirrorMove(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        if (victim.previousMoveData is {failedAttempt:false})
        {
            var nonCopyableMoves = new[] {"Detect","Protect","Haze"};
            if (victim.previousMoveData.move.isSelfTargeted
                || nonCopyableMoves.Contains(victim.previousMoveData.move.moveName))
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

    private IEnumerator Whirlwind(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        if (attacker.pokemon.currentLevel < victim.pokemon.currentLevel)
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            yield break;
        }
        if (!_battleHandler.isTrainerBattle)
        {
            _battleHandler.EndBattle(BattleEndState.BattleTerminated,null);
            _moveUsageHandler.ResetAfterBattleTermination();
            yield break;
        }
        
        var partyPositionOfVictim = victim.participantKey < victim.GetPartnerKey()? 0 : 1;
        if (victim.isPlayer)
        {
            yield return CreateSwitchData(_pokemonPartyHandler.GetLivingPokemon(), _pokemonPartyHandler.Party.ToList());
        }
        else
        {
            var enemyTrainer = victim.pokemonTrainerAI;
            yield return CreateSwitchData(enemyTrainer.GetLivingPokemon(), enemyTrainer.trainerParty);
        }

        IEnumerator CreateSwitchData(List<Pokemon> living, List<Pokemon>  fullParty)
        {
            if (living.Count == 1)
            {
                _dialogueHandler.DisplayBattleInfo("but it failed!");
                yield break;
            }
            //exclude current participants
            var excludedIndexes = 1;

            if (_battleHandler.isDoubleBattle)
                excludedIndexes++;
            
            var randomIndexOfLiving = Utility.RandomRange(excludedIndexes, living.Count);
            
            var pokemonIndex = fullParty.IndexOf(living[randomIndexOfLiving]);
            
            var switchData = new SwitchOutData(partyPositionOfVictim,pokemonIndex,victim);

            yield return _turnBasedCombatHandler.HandleSwap(switchData,true);
        }
    }
    private IEnumerator Rest(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        var healthGain = attacker.pokemon.maxHp - attacker.pokemon.hp;
        _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" fell asleep!");
        yield return new WaitForSeconds(1f);
        _moveUsageHandler.HealthGainDisplay(healthGain,healthGainer:attacker);
        yield return _moveUsageHandler.AwaitHealthGainDisplay();
        attacker.statusHandler.RemoveStatusEffect(true);
        yield return new WaitUntil(()=>attacker.pokemon.statusEffect == StatusEffect.None);
        _moveUsageHandler.ApplyStatusToVictim(attacker, StatusEffect.Sleep, 2);
        yield return _dialogueHandler.AwaitAllDialogue();
    }
}
