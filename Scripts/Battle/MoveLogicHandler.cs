using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class MoveLogicHandler : MonoBehaviour,IInjectable
{
    private Dialogue_handler _dialogueHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Battle_handler _battleHandler;
    private Move_handler _moveUsageHandler;
    private MoveLogicDatabase _moveLogicDatabase;
    private BattleOperations _battleOperations;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _moveUsageHandler = container.Resolve<Move_handler>();
        _moveLogicDatabase = container.Resolve<MoveLogicDatabase>();
        _battleOperations = container.Resolve<BattleOperations>();
        gameObject.SetActive(true);
    }
    
    public void OnInject()
    {
        
    }
    public IEnumerator DetermineMoveLogic(Battle_Participant attacker, Battle_Participant victim, Turn currentTurn)
    {
        var move = currentTurn.move;
        switch (currentTurn.move.effectType)
        {
            case EffectType.MultiTargetDamage:
               yield return HandleMultiTargetDamage(move,attacker); 
               break;
            case EffectType.Consecutive:
                yield return ExecuteConsecutiveMove(move,attacker,victim); 
                break;
            case EffectType.HealthDrain:
                yield return DrainHealth(move,attacker,victim); 
                break;
            case EffectType.DamageProtection:
                yield return ApplyDamageProtection(move,attacker); 
                break;
            case EffectType.WeatherHealthGain:
                yield return HealFromWeather(attacker); 
                break;
            case EffectType.IdentifyTarget:
                yield return IdentifyTarget(attacker,victim); 
                break;
            case EffectType.BarrierCreation:
                yield return CreateBarriers(move,attacker); 
                break;
            case EffectType.OnFieldDamageModifier:
                yield return OnFieldDamageModLogic(move,attacker); 
                break;
            case EffectType.SemiInvulnerable:
                yield return ExecuteSemiInvulnerableMove(currentTurn,attacker,victim); 
                break;
            case EffectType.WeatherChange:
                yield return ChangeWeather(move); 
                break;
            case EffectType.UniqueLogic:
                yield return _moveLogicDatabase.InvokeMoveLogic(attacker,victim,currentTurn); 
                break;
        }
    }

    public List<Battle_Participant> TargetAllExceptSelf(Battle_Participant attacker)
    {
        var allParticipants = _battleHandler.GetParticipants.ToList();
        allParticipants.RemoveAll(p => !p.isActive);
        allParticipants.RemoveAll(p => p.pokemon.pokemonID == attacker.pokemon.pokemonID);
        return allParticipants;
    }
    IEnumerator ExecuteConsecutiveMove(Move move,Battle_Participant attacker, Battle_Participant victim)
    {
        var consecutiveMoveInfo = move.GetModule<ConsecutiveMoveInfo>();
        if (consecutiveMoveInfo.isRandomHitCount)
        {
            consecutiveMoveInfo.numHits = Utility.RandomRange(1, 6);
        }
        
        var numHits = 0;
        for (int i = 0; i < consecutiveMoveInfo.numHits; i++)
        {
            if (!victim.canBeDamaged)
            {
                _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName+" protected itself");
                break;
            }
            if (victim.pokemon.hp <= 0) break;
            
            _dialogueHandler.DisplayBattleInfo("Hit "+(i+1)+"!");//remove later if added animations
            _moveUsageHandler.DisplayMoveDamage(move,attacker,victim,displayEffectiveness:false);
            yield return _moveUsageHandler.AwaitDamageDisplay();
            numHits++;
            yield return _dialogueHandler.AwaitAllDialogue();
        }
        if (numHits>0 && consecutiveMoveInfo.displayHitCount && victim.pokemon.hp > 0)
        {
            _moveUsageHandler.DisplayEffectiveness
                (_battleOperations.CheckTypeEffectiveness(victim, move.type), victim);
            _dialogueHandler.DisplayBattleInfo("It hit (x" + numHits + ") times");
        }
        yield return _dialogueHandler.AwaitAllDialogue();
    } 
    public IEnumerator ApplyMultiTargetDamage(List<Battle_Participant> targets,Move move,Battle_Participant attacker)
    {
        yield return _dialogueHandler.AwaitAllDialogue();
        foreach (var enemy in targets)
        {
            if (!enemy.isActive) continue;
            _moveUsageHandler.DisplayMoveDamage(move,attacker,enemy);
            yield return _moveUsageHandler.AwaitDamageDisplay();
            yield return _battleHandler.AwaitFaintQueue();
            yield return _dialogueHandler.AwaitAllDialogue();
        }
        yield return _dialogueHandler.AwaitAllDialogue();
    }
    IEnumerator HandleMultiTargetDamage(Move move,Battle_Participant attacker)
    {
        var multiTargetInfo = move.GetModule<MultiTargetDamageInfo>();
        var targets = new List<Battle_Participant>();
        switch (multiTargetInfo.target)
        {
            case Target.AllEnemies :
                targets = attacker.currentEnemies;
                break;
            case Target.AllExceptSelf :
                targets = TargetAllExceptSelf(attacker);
                break;
        }
        yield return ApplyMultiTargetDamage(targets,move,attacker);
    }

    IEnumerator DrainHealth(Move move,Battle_Participant attacker, Battle_Participant victim)
    {
        var healthDrainInfo = move.GetModule<HealthDrainMoveInfo>();
        var damage = _moveUsageHandler.CalculateMoveDamage(move,attacker,victim);
        var healAmount = victim.pokemon.hp-damage<=0 ? victim.pokemon.hp : damage; 
        healAmount *= healthDrainInfo.percentageOfDamage/100f;
        
        _moveUsageHandler.DisplaySpecificMoveDamage(move,victim,damage);
        
        yield return _moveUsageHandler.AwaitDamageDisplay();

        if (attacker.pokemon.hp >= attacker.pokemon.maxHp)
        {
            yield break;
        }
        
        _moveUsageHandler.HealthGainDisplay(healAmount,healthGainer:attacker);
        _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" gained health");
        yield return _dialogueHandler.AwaitAllDialogue();
        yield return _moveUsageHandler.AwaitHealthGainDisplay();
    }

    private IEnumerator ApplyDamageProtection(Move move,Battle_Participant attacker)
    {
        if(attacker.previousMoveData.move.moveName == move.moveName)
        {
            int chance = 100;
            for (int i = 0; i < attacker.previousMoveData.numRepetitions; i++)
                chance /= 2;
            if (Utility.RandomRange(1, 101) <= chance)
                attacker.canBeDamaged = false;
            else
            {
                attacker.canBeDamaged = true;
                _dialogueHandler.DisplayBattleInfo("It failed!");
            }
        }
        else
            attacker.canBeDamaged = false;
        yield return null;
    }

    private IEnumerator CreateBarriers(Move move,Battle_Participant attacker)
    {
        var barrierName = move.moveName;
        if (_battleHandler.isDoubleBattle)
        {
            if (!_moveUsageHandler.HasDuplicateBarrier(attacker, barrierName, true))
            {
                var newBarrier = new Barrier(barrierName, 0.33f, 5);
                
                attacker.barriers.Add(newBarrier);

                var partner = attacker.GetPartner();

                if (partner.isActive)
                {
                    var barrierCopy = new Barrier(newBarrier.barrierName, newBarrier.barrierEffect, newBarrier.barrierDuration);
                    partner.barriers.Add(barrierCopy);
                }
                
                _dialogueHandler.DisplayBattleInfo(barrierName + " has been activated");
                yield return _dialogueHandler.AwaitAllDialogue();
            }
        }
        else
        {
            if (_moveUsageHandler.HasDuplicateBarrier(attacker, barrierName,true))
                yield return _dialogueHandler.AwaitAllDialogue();
            else
            {
                attacker.barriers.Add(new(barrierName,0.33f,5));
                
                _dialogueHandler.DisplayBattleInfo(barrierName + " has been activated");
            }
        }
        
        yield return _dialogueHandler.AwaitAllDialogue();
    }
    
    private IEnumerator OnFieldDamageModLogic(Move move,Battle_Participant attacker)
    {
        var damageModifierInfo = move.GetModule<DamageModifierInfo>();
        _dialogueHandler.DisplayBattleInfo(damageModifierInfo.damageChangeMessage);
        if (_moveUsageHandler.DamageModifierPresent(damageModifierInfo.typeAffected))
        {
            yield break;
        } 
        var damageModifier = new OnFieldDamageModifier(_battleHandler,_moveUsageHandler,_turnBasedCombatHandler,damageModifierInfo,attacker);
        
        _battleHandler.OnParticipantFainted += RemoveOnFaint;
                
        void RemoveOnFaint(Battle_Participant faintedParticipant)
        {
            if (faintedParticipant != attacker) return;
            _battleHandler.OnParticipantFainted -= RemoveOnFaint;
            damageModifier.RemoveOnSwitchOut(attacker);
        }
        
        _battleHandler.OnSwitchOut += damageModifier.RemoveOnSwitchOut;
        _moveUsageHandler.AddFieldDamageModifier(damageModifier);
    }
    private IEnumerator IdentifyTarget(Battle_Participant attacker, Battle_Participant victim)
    {
        if (victim.immunityNegations.Any(n=> 
                n.moveName==ImmunityNegationMove.Foresight))
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            yield break;
        }
        _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName +" was identified!");
        victim.pokemon.buffAndDebuffs
            .RemoveAll(b => b.stat == Stat.Evasion);
        victim.pokemon.evasion = 100;
        if(victim.pokemon.HasType(PokemonType.Ghost))
        {
            var newImmunityNegation = new TypeImmunityNegation(_battleHandler,ImmunityNegationMove.Foresight
                , attacker, victim);

            newImmunityNegation.ImmunityNegationTypes.Add(PokemonType.Fighting);
            newImmunityNegation.ImmunityNegationTypes.Add(PokemonType.Normal);
            
            _battleHandler.OnParticipantFainted += RemoveOnFaint;
                
            void RemoveOnFaint(Battle_Participant faintedParticipant)
            {
                if (faintedParticipant != attacker) return;
                _battleHandler.OnParticipantFainted -= RemoveOnFaint;
                newImmunityNegation.RemoveNegationOnSwitchOut(attacker);
            }

            _battleHandler.OnSwitchOut += newImmunityNegation.RemoveNegationOnSwitchOut;
            victim.immunityNegations.Add(newImmunityNegation);
        }
    }
    private IEnumerator ExecuteSemiInvulnerableMove(Turn currentTurn,Battle_Participant attacker, Battle_Participant victim)
    {
        var move = currentTurn.move;
        if (attacker.semiInvulnerabilityData.executionTurn)
        {
            _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName
                                                        + attacker.semiInvulnerabilityData.onHitMessage);
            _moveUsageHandler.DisplayMoveDamage(move,attacker,victim);
            attacker.semiInvulnerabilityData.executionTurn = false;
            yield break;
        }

        var semiInvulnerableData = move.GetModule<SemiInvulnerabilityInfo>();
        
        attacker.semiInvulnerabilityData.displayMessage = semiInvulnerableData.displayMessage;
        attacker.semiInvulnerabilityData.onHitMessage = semiInvulnerableData.onHitMessage;
        attacker.semiInvulnerabilityData.turnData = new Turn(currentTurn);

        attacker.semiInvulnerabilityData.semiInvulnerabilities
            .AddRange(semiInvulnerableData.semiInvulnerabilities);

        attacker.isSemiInvulnerable = true;
        move.isSureHit = false;
        attacker.semiInvulnerabilityData.executionTurn = true;
        _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+semiInvulnerableData.executionMessage);
    }
    
    IEnumerator ChangeWeather(Move move)
    {
        var weatherInfo = move.GetModule<ChangeWeatherInfo>();;
        var newWeather = new WeatherCondition(weatherInfo.newWeatherCondition);
        _turnBasedCombatHandler.ChangeWeather(newWeather);
        yield return null;
    }
    private IEnumerator HealFromWeather(Battle_Participant attacker)
    {
        if (attacker.pokemon.hp >= attacker.pokemon.maxHp)
        {
            _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+"'s health is already full!");
            yield break;
        }
        float fraction;
        var currentWeather = _turnBasedCombatHandler.currentWeather.weather;
        
        switch (currentWeather)
        {
            case Weather.Sunlight:
                fraction = 2f / 3f;  
                break;
            case Weather.Rain:
            case Weather.Hail:
            case Weather.Sandstorm:
                fraction = 1f / 4f;          
                break;
            default: 
                fraction = 1f / 2f; 
                break;
        }
        int healthGain = Mathf.FloorToInt(attacker.pokemon.maxHp * fraction);
        
        if (healthGain < 1 && attacker.pokemon.hp < attacker.pokemon.maxHp) healthGain = 1;
        
        _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+" restored it's health!");

        _moveUsageHandler.HealthGainDisplay(healthGain,healthGainer:attacker);
        yield return _moveUsageHandler.AwaitHealthGainDisplay();
    }


    public IEnumerator Pursuit(Battle_Participant pursuitUser,Battle_Participant switchOutVictim,Move pursuit)
    {
        _dialogueHandler.DisplayBattleInfo(pursuitUser.pokemon.pokemonDisplayName+" used "+pursuit.moveName
                                                    +" on "+switchOutVictim.pokemon.pokemonDisplayName+"!");

        var pursuitDamage = _moveUsageHandler.CalculateMoveDamage(pursuit,pursuitUser, switchOutVictim) * 2;
        
        _moveUsageHandler.DisplaySpecialDamage(switchOutVictim,predefinedDamage:pursuitDamage);
        yield return _moveUsageHandler.AwaitDamageDisplay();
        yield return _dialogueHandler.AwaitAllDialogue();      
    }

}
