using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class MoveLogicDatabase : MonoBehaviour,IInjectable
{
    private Turn _currentTurn;
    private Battle_Participant _attacker;
    private Battle_Participant _victim;
    
    private Dialogue_handler _dialogueHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Pokemon_party _pokemonPartyHandler;
    private WildPokemonAiHandler _wildPokemonHandler;
    private BattleVisuals _battleVisualsHandler;
    private Battle_handler _battleHandler;
    private Move_handler _moveUsageHandler;
    private BattleOperations _battleOperationsHandler;
    private MoveLogicHandler _moveLogicHandler;

    private Dictionary<string, Func<IEnumerator>> _logicMethods = new();
    
    public void Inject(ServiceContainer container)
    {
        _battleOperationsHandler = container.Resolve<BattleOperations>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _battleHandler = container.Resolve<Battle_handler>();
        _wildPokemonHandler = container.Resolve<WildPokemonAiHandler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _moveUsageHandler = container.Resolve<Move_handler>();
        _moveLogicHandler = container.Resolve<MoveLogicHandler>();
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
    {
        _logicMethods.Add("brickbreak", BrickBreak);
        _logicMethods.Add("haze", Haze);
        _logicMethods.Add("hyperbeam", Hyperbeam);
        _logicMethods.Add("bide", Bide);
        _logicMethods.Add("sonicboom", Sonicboom);
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
    public IEnumerator InvokeMoveLogic(string moveName,Battle_Participant attacker, Battle_Participant victim, Turn currentTurn)
    {
        _attacker = attacker;
        _victim = victim;
        _currentTurn = currentTurn;
        var formattedName = moveName.Replace(" ", "").ToLower();
        
        if (_logicMethods.TryGetValue(formattedName, out var abilityMethod))
        {
            yield return abilityMethod();
        }
        else
            Debug.LogWarning($"Move '{formattedName}' not found!");
    }
    private IEnumerator BrickBreak()
    {
        var duplicateBarriers = new List<string>();
        foreach (var enemy in _attacker.currentEnemies)
        {
            if(!enemy.isActive)continue;
            foreach (var barrier in enemy.barriers)
            {
                if (duplicateBarriers.Contains(barrier.barrierName)) continue;
                _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName+" shattered "+barrier.barrierName);
                duplicateBarriers.Add(barrier.barrierName);
            }
            enemy.barriers.Clear();
        }
        
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        _moveUsageHandler.DisplayDamage(_victim);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
    }
    
    private IEnumerator Haze()
    {
        var validParticipants = _battleHandler.GetValidParticipants();
        foreach (var participant in validParticipants)
        {
            participant.pokemon.buffAndDebuffs.Clear();
            participant.statData.LoadActualStats();
        }
        yield return null;
    }

    private IEnumerator Hyperbeam()
    {
        _moveUsageHandler.DisplayDamage(_victim);
        var cancelledTurn = new Turn(_currentTurn);
        cancelledTurn.isCancelled = true;
        _attacker.currentCoolDown.UpdateCoolDown( 1,cancelledTurn,message: " must recharge!");
        yield return null;
    }

    private IEnumerator Bide()
    {
        if (_attacker.currentCoolDown.isExecutionTurn)
        {
            _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName+" unleashed the power");
            if (_attacker.currentCoolDown.turnData.move.moveDamage > 0)
            {
                _currentTurn.move.moveDamage = _attacker.currentCoolDown.turnData.move.moveDamage;
                var typelessDamage = _moveUsageHandler.CalculateMoveDamage(_currentTurn.move, _victim, true);
                _moveUsageHandler.DisplayDamage(_victim,displayEffectiveness:false,isSpecificDamage:true
                    ,predefinedDamage:typelessDamage);
            }
            else
            {
                _dialogueHandler.DisplayBattleInfo("But it failed!");
            }
           
            _moveUsageHandler.OnDamageDeal -= _attacker.currentCoolDown.StoreDamage;
            _attacker.currentCoolDown.ResetState();
            yield return null;
        }
        else
        {
            _attacker.currentCoolDown.UpdateCoolDown(2,_currentTurn, " is storing power");//change turns back
            _moveUsageHandler.OnDamageDeal += _attacker.currentCoolDown.StoreDamage;
        }
       
    }

    private IEnumerator Sonicboom()
    {
        var sonicBoomDamage = 20f;
        _moveUsageHandler.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:sonicBoomDamage);
        yield return null;
    }    
    private IEnumerator TakeDown()
    {
        var damage = _moveUsageHandler.CalculateMoveDamage(_currentTurn.move, _victim);
        var recoilDamage = math.floor(damage / 4f);
        _moveUsageHandler.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:damage);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
        _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName +" was hurt by the recoil");
        _moveUsageHandler.DisplayDamage(_attacker,isSpecificDamage:true
            ,predefinedDamage:recoilDamage,displayEffectiveness: false);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
    }

    private IEnumerator Magnitude()
    {
        var magnitudeStrength = Utility.RandomRange(4, 11);
        var baseDamage = 10f;
        var damageIncrease = 0f;
        if(magnitudeStrength > 4)
            damageIncrease = 20f;
        baseDamage += damageIncrease * (magnitudeStrength - 4);
        if (magnitudeStrength == 10)
            baseDamage += 20f;
        _dialogueHandler.DisplayBattleInfo("Magnitude level "+magnitudeStrength);
        _currentTurn.move.moveDamage = baseDamage;
        yield return _moveLogicHandler.ApplyMultiTargetDamage(_moveLogicHandler.TargetAllExceptSelf());
    }

    private IEnumerator Endeavor()
    {
        if (_victim.pokemon.hp < _attacker.pokemon.hp)
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            yield break;
        }
        var damage = _victim.pokemon.hp - _attacker.pokemon.hp;
        _moveUsageHandler.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:damage);
    }

    private IEnumerator FuryCutter()
    {
        var damageLevel = new[] { 10f, 20f, 40f, 80f, 160f };
        if (_attacker.previousMove.move.moveName == NameDB.GetMoveName(LearnSetMoveName.FuryCutter))
        {
            _currentTurn.move.moveDamage = _attacker.previousMove.numRepetitions > 4?
                damageLevel[^1] : damageLevel[_attacker.previousMove.numRepetitions];
        }
        else
            _currentTurn.move.moveDamage = damageLevel[0];
        _moveUsageHandler.DisplayDamage(_victim);
        yield return null;
    }
    private IEnumerator Silverwind()
    {
        bool battleEnded = false;
        
        void CancelOnBattleEnd()
        {
            battleEnded = !_battleHandler.isTrainerBattle || _battleHandler.battleOver;
        }
        _victim.OnPokemonFainted += CancelOnBattleEnd;
        
        _moveUsageHandler.DisplayDamage(_victim);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
        
        if(battleEnded) yield break;
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
        
        var waiting = true;
        void AwaitBuffAddition()
        {
            _battleOperationsHandler.OnBuffApplied -= AwaitBuffAddition;
            waiting = false;
        }
        void AwaitBuffVisual()
        {
            _battleVisualsHandler.OnStatVisualDisplayed -= AwaitBuffVisual;
            waiting = false;
        }

        var statChangeMessage = "";
        _battleOperationsHandler.canDisplayChange = false;
        foreach (var buff in allBuffs)
        {
            waiting = true;
            _battleOperationsHandler.OnBuffApplied += AwaitBuffAddition;
            var buffData = new BuffDebuffData(_attacker, buff, true, 1);
            _moveUsageHandler.ExecuteBuffOrDebuff(buffData);
            yield return new WaitUntil(() => !waiting);
        }
        
        statChangeMessage = BattleOperations.GetBuffResultMessage(true,_attacker.pokemon,allBuffs);
        _battleVisualsHandler.OnStatVisualDisplayed += AwaitBuffVisual;
        waiting = true;
        _battleVisualsHandler.SelectStatChangeVisuals(Stat.Multi,_attacker,statChangeMessage);
        yield return new WaitUntil(() => !waiting);
    }

    private IEnumerator Flail()
    {
        List<(int hpLevel, float damage)> damagePerLevel = new()
        {
            (32, 200f), (16, 150f), (8, 100f), (4, 80f), (2, 40f)
        };

        var currentHpRatio = _attacker.pokemon.hp / _attacker.pokemon.maxHp;

        foreach (var phase in damagePerLevel)
        {
            if (currentHpRatio <= 1f / phase.hpLevel)
            {
                _currentTurn.move.moveDamage = phase.damage;
                break;
            }
        }
        _moveUsageHandler.DisplayDamage(_victim);
        yield return null;
    }

    private IEnumerator FalseSwipe()
    {
        var damage = _moveUsageHandler.CalculateMoveDamage(_currentTurn.move, _victim);
        if (_victim.pokemon.hp - damage <= 0)
        {
            damage = _victim.pokemon.hp - 1;
        }
        _moveUsageHandler.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:damage);
        yield return null;
    }

    private IEnumerator BellyDrum()
    {
        if (_attacker.pokemon.hp < 2)
        {
            _dialogueHandler.DisplayBattleInfo("But it failed!");
            yield break;
        }
        
        var selfDamage = math.floor(_attacker.pokemon.hp / 2f);
        _moveUsageHandler.DisplayDamage(_attacker,displayEffectiveness:false,
            isSpecificDamage:true,predefinedDamage:selfDamage);
        
        var buffData = new BuffDebuffData(_attacker, Stat.Attack, true, 6);
        _moveUsageHandler.ExecuteBuffOrDebuff(buffData);
    }

    private IEnumerator Covet()
    {
        _moveUsageHandler.DisplayDamage(_victim);
        if (_victim.pokemon.hasItem && !_attacker.pokemon.hasItem)
        {
            if (_victim.pokemon.heldItem.itemType == ItemType.Berry)
            {
                _attacker.pokemon.GiveItem(Obj_Instance.CreateItem(_victim.pokemon.heldItem));
                _victim.pokemon.RemoveHeldItem();
            }
        }
        yield return null;
    }

    private IEnumerator MirrorMove()
    {
        if (_victim.previousMove is {failedAttempt:false})
        {
            var nonCopyableMoves = new[] {"Detect","Protect","Haze"};
            if (_victim.previousMove.move.isSelfTargeted
                || nonCopyableMoves.Contains(_victim.previousMove.move.moveName))
            {
                _dialogueHandler.DisplayBattleInfo("But it failed!");
                yield break;
            }
            _moveUsageHandler.repeatingMoveCycle = true;
            _currentTurn.move = _victim.previousMove.move;
            _dialogueHandler.DisplayBattleInfo(
                _turnBasedCombatHandler.GetMoveUsageText(_currentTurn.move,_attacker, _victim));
            _moveUsageHandler.OnMoveComplete += ()=> _moveUsageHandler.ExecuteMove(_currentTurn);
        }
        else
        {
            _dialogueHandler.DisplayBattleInfo("But it failed!");
        }
    }

    private IEnumerator Whirlwind()
    {
        if (_attacker.pokemon.currentLevel<_victim.pokemon.currentLevel)
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            yield break;
        }
        if (!_battleHandler.isTrainerBattle)
        {
            _wildPokemonHandler.inBattle = false;
            _battleHandler.EndBattle(false,true);
            _moveUsageHandler.doingMove = false;
            yield break;
        }
        if (_victim.isPlayer)
        {
            var living = _pokemonPartyHandler.GetLivingPokemon();
            if (living.Count < 2)
            {
                _dialogueHandler.DisplayBattleInfo("but it failed!");
                yield break;
            }
            
            //exclude current participants
            var excludedIndexes = 1;

            if (_battleHandler.isDoubleBattle)
                excludedIndexes++;
            
            var randomIndexOfLiving = Utility
                .RandomRange(excludedIndexes, living.Count);
            
            var pokemonAtIndex = Array.IndexOf(_pokemonPartyHandler.party,living[randomIndexOfLiving]);
            var switchData = new SwitchOutData(_currentTurn.victimIndex,pokemonAtIndex,_victim);
            
            yield return _turnBasedCombatHandler.HandleSwap(switchData,true);
        }
        else
        {
            var enemyTrainer = _victim.pokemonTrainerAI;
            var living = enemyTrainer.GetLivingPokemon();
            if (living.Count < 2)
            {
                _dialogueHandler.DisplayBattleInfo("but it failed!");
                yield break;
            }

            //exclude current participants
            var excludedIndexes = 1;

            if (_battleHandler.isDoubleBattle)
                excludedIndexes++;
            
            var randomIndexOfLiving = Utility
                .RandomRange(excludedIndexes, living.Count);
            
            var pokemonAtIndex = enemyTrainer.trainerParty.IndexOf(living[randomIndexOfLiving]);
            
            var switchData = new SwitchOutData(_currentTurn.victimIndex,pokemonAtIndex,_victim);

            yield return _turnBasedCombatHandler.HandleSwap(switchData,true);
        }
    }
    private IEnumerator Rest()
    {
        var healthGain = _attacker.pokemon.maxHp - _attacker.pokemon.hp;
        _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName+" fell asleep!");
        yield return new WaitForSeconds(1f);
        _moveUsageHandler.HealthGainDisplay(healthGain,healthGainer:_attacker);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingHealthGain);
        _attacker.statusHandler.RemoveStatusEffect(true);
        yield return new WaitUntil(()=>_attacker.pokemon.statusEffect == StatusEffect.None);
        _moveUsageHandler.ApplyStatusToVictim(_attacker, StatusEffect.Sleep, 2);
        yield return new WaitUntil(()=>!_dialogueHandler.messagesLoading);
    }
}
