using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveLogicHandler : MonoBehaviour,IInjectable
{
    private DialogueHandler _dialogueHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
    private MoveLogicDatabase _moveLogicDatabase;
    private BattleOperations _battleOperations;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        _moveLogicDatabase = container.Resolve<MoveLogicDatabase>();
        _battleOperations = container.Resolve<BattleOperations>();
        gameObject.SetActive(true);
    }
    
    public void OnInject()
    {
        
    }
    public IEnumerator DetermineMoveLogic(BattleParticipant attacker, BattleParticipant victim, Turn currentTurn)
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
                yield return IdentifyTarget(move,attacker,victim); 
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

    public List<BattleParticipant> TargetAllExceptSelf(BattleParticipant attacker)
    {
        var allParticipants = _battleHandler.GetParticipants.ToList();
        allParticipants.RemoveAll(p => !p.isActive);
        allParticipants.RemoveAll(p => p.participantKey == attacker.participantKey);
        return allParticipants;
    }
    IEnumerator ExecuteConsecutiveMove(Move move,BattleParticipant attacker, BattleParticipant victim)
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
    public IEnumerator ApplyMultiTargetDamage(List<BattleParticipant> targets,Move move,BattleParticipant attacker)
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
    IEnumerator HandleMultiTargetDamage(Move move,BattleParticipant attacker)
    {
        var multiTargetInfo = move.GetModule<MultiTargetDamageInfo>();
        var targets = new List<BattleParticipant>();
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

    IEnumerator DrainHealth(Move move,BattleParticipant attacker, BattleParticipant victim)
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

    private IEnumerator ApplyDamageProtection(Move move,BattleParticipant attacker)
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

    private IEnumerator CreateBarriers(Move move,BattleParticipant attacker)
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
    
    private IEnumerator OnFieldDamageModLogic(Move move,BattleParticipant attacker)
    {
        var damageModifierInfo = move.GetModule<DamageModifierInfo>();
        
        if (_moveUsageHandler.FieldDamageSourceExists(damageModifierInfo.modifierSource))
        {
            _dialogueHandler.DisplayBattleInfo("But it failed!");
            yield break;
        }
        
        var damageModifier = new OnFieldDamageModifier(_battleHandler,_moveUsageHandler,
            _turnBasedCombatHandler,damageModifierInfo,attacker);
        
        _dialogueHandler.DisplayBattleInfo(damageModifierInfo.damageChangeMessage);
        
        _battleHandler.OnParticipantFainted += RemoveOnFaint;
                
        void RemoveOnFaint(BattleParticipant faintedParticipant)
        {
            if (faintedParticipant != attacker) return;
            _battleHandler.OnParticipantFainted -= RemoveOnFaint;
            damageModifier.RemoveOnSwitchOut(attacker);
        }
        
        _battleHandler.OnSwitchOut += damageModifier.RemoveOnSwitchOut;
        _moveUsageHandler.AddFieldDamageModifier(damageModifier);
        
        yield return null;
    }
    private IEnumerator IdentifyTarget(Move move,BattleParticipant attacker, BattleParticipant victim)
    {
        LearnSetMoveName currentMoveEnum = NameDB.ParseMoveName(move.moveName);
        
        if (victim.immunityNegations.Any(negation => negation.moveName == currentMoveEnum))
        {
            //already in effect
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            yield break;
        }
        
        _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName +" was identified!");
        
        if(currentMoveEnum == LearnSetMoveName.Foresight)
        {
            victim.pokemon.statModifiers
                .RemoveAll(b => b.stat == Stat.Evasion);
            victim.pokemon.evasion = 100;
        }
        
        if(victim.pokemon.HasType(PokemonType.Ghost))
        {
            var newImmunityNegation = new TypeImmunityNegation(_battleHandler
                ,currentMoveEnum
                , attacker, victim);

            newImmunityNegation.ImmunityNegationTypes.Add(PokemonType.Fighting);
            newImmunityNegation.ImmunityNegationTypes.Add(PokemonType.Normal);
            
            _battleHandler.OnParticipantFainted += RemoveOnFaint;
                
            void RemoveOnFaint(BattleParticipant faintedParticipant)
            {
                if (faintedParticipant != attacker) return;
                _battleHandler.OnParticipantFainted -= RemoveOnFaint;
                newImmunityNegation.RemoveNegationOnSwitchOut(attacker);
            }

            _battleHandler.OnSwitchOut += newImmunityNegation.RemoveNegationOnSwitchOut;
            victim.immunityNegations.Add(newImmunityNegation);
        }
    }
    private IEnumerator ExecuteSemiInvulnerableMove(Turn currentTurn,BattleParticipant attacker, BattleParticipant victim)
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
        var weatherInfo = move.GetModule<ChangeWeatherInfo>();
        var newWeather = new WeatherCondition(weatherInfo.newWeatherCondition);
        _turnBasedCombatHandler.ChangeWeather(newWeather);
        yield return null;
    }
    private IEnumerator HealFromWeather(BattleParticipant attacker)
    {
        if (attacker.pokemon.hp >= attacker.pokemon.maxHp)
        {
            _dialogueHandler.DisplayBattleInfo(attacker.pokemon.pokemonDisplayName+"'s health is already full!");
            yield break;
        }
        float fraction;
        var currentWeather = _turnBasedCombatHandler.CurrentWeather.weather;
        
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


    public IEnumerator Pursuit(BattleParticipant pursuitUser,BattleParticipant switchOutVictim,Move pursuit)
    {
        _dialogueHandler.DisplayBattleInfo(pursuitUser.pokemon.pokemonDisplayName+" used "+pursuit.moveName
                                                    +" on "+switchOutVictim.pokemon.pokemonDisplayName+"!");

        var pursuitDamage = _moveUsageHandler.CalculateMoveDamage(pursuit,pursuitUser, switchOutVictim) * 2;
        
        _moveUsageHandler.DisplaySpecialDamage(switchOutVictim,predefinedDamage:pursuitDamage);
        yield return _moveUsageHandler.AwaitDamageDisplay();
        yield return _dialogueHandler.AwaitAllDialogue();      
    }

}
