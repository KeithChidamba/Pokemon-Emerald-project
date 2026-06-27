using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class MoveLogicHandler : MonoBehaviour,IInjectable
{
    private Turn _currentTurn;
    private Battle_Participant _attacker;
    private Battle_Participant _victim;
    public bool moveDelay;
    
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
        _attacker = attacker;
        _victim = victim;
        _currentTurn = currentTurn;
        
        moveDelay = true;
        switch (currentTurn.move.effectType)
        {
            case EffectType.MultiTargetDamage:
               yield return HandleMultiTargetDamage(); 
               break;
            case EffectType.Consecutive:
                yield return ExecuteConsecutiveMove(); 
                break;
            case EffectType.HealthDrain:
                yield return DrainHealth(); 
                break;
            case EffectType.DamageProtection:
                yield return ApplyDamageProtection(); 
                break;
            case EffectType.WeatherHealthGain:
                yield return HealFromWeather(); 
                break;
            case EffectType.IdentifyTarget:
                yield return IdentifyTarget(); 
                break;
            case EffectType.BarrierCreation:
                yield return CreateBarriers(); 
                break;
            case EffectType.OnFieldDamageModifier:
                yield return OnFieldDamageModLogic(); 
                break;
            case EffectType.SemiInvulnerable:
                yield return ExecuteSemiInvulnerableMove(); 
                break;
            case EffectType.WeatherChange:
                yield return ChangeWeather(); 
                break;
            case EffectType.UniqueLogic:
                yield return HandleUniqueLogic(); 
                break;
        }
        yield return new WaitUntil(() => !moveDelay);
    }

    private IEnumerator HandleUniqueLogic()
    {
        yield return _moveLogicDatabase.InvokeMoveLogic(_currentTurn.move.moveName,_attacker,_victim,_currentTurn);
        moveDelay = false;
    }
    public List<Battle_Participant> TargetAllExceptSelf()
    {
        var allParticipants = _battleHandler.battleParticipants.ToList();
        allParticipants.RemoveAll(p => !p.isActive);
        allParticipants.RemoveAll(p => p.pokemon.pokemonID == _attacker.pokemon.pokemonID);
        return allParticipants;
    }
    IEnumerator ExecuteConsecutiveMove()
    {
        var consecutiveMoveInfo = _currentTurn.move.GetModule<ConsecutiveMoveInfo>();
        if (consecutiveMoveInfo.isRandomHitCount)
        {
            consecutiveMoveInfo.numHits = Utility.RandomRange(1, 6);
        }
        
        var numHits = 0;
        for (int i = 0; i < consecutiveMoveInfo.numHits; i++)
        {
            if (!_victim.canBeDamaged)
            {
                _dialogueHandler.DisplayBattleInfo(_victim.pokemon.pokemonDisplayName+" protected itself");
                break;
            }
            if (_victim.pokemon.hp <= 0) break;
            
            _dialogueHandler.DisplayBattleInfo("Hit "+(i+1)+"!");//remove later if added animations
            _moveUsageHandler.DisplayDamage(_victim,false);
            yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
            numHits++;
            yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        }
        if (numHits>0 && consecutiveMoveInfo.displayHitCount && _victim.pokemon.hp > 0)
        {
            _moveUsageHandler.DisplayEffectiveness
                (_battleOperations.CheckTypeEffectiveness(_victim, _currentTurn.move.type), _victim);
            _dialogueHandler.DisplayBattleInfo("It hit (x" + numHits + ") times");
        }
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        moveDelay = false;
    } 
    public IEnumerator ApplyMultiTargetDamage(List<Battle_Participant> targets)
    {
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        foreach (var enemy in targets)
        {
            if (!enemy.isActive) continue;
            _moveUsageHandler.DisplayDamage(enemy);
            yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
            yield return new WaitUntil(() => !_turnBasedCombatHandler.faintEventDelay && _battleHandler.faintQueue.Count == 0);
            yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        }
        yield return new WaitUntil(() => _battleHandler.faintQueue.Count == 0 && !_turnBasedCombatHandler.faintEventDelay);
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        moveDelay = false;
    }
    IEnumerator HandleMultiTargetDamage()
    {
        var multiTargetInfo = _currentTurn.move.GetModule<MultiTargetDamageInfo>();
        var targets = new List<Battle_Participant>();
        switch (multiTargetInfo.target)
        {
            case Target.AllEnemies :
                targets = _attacker.currentEnemies;
                break;
            case Target.AllExceptSelf :
                targets = TargetAllExceptSelf();
                break;
        }
        yield return ApplyMultiTargetDamage(targets);
    }

    IEnumerator DrainHealth()
    {
        var healthDrainInfo = _currentTurn.move.GetModule<HealthDrainMoveInfo>();
        var damage = _moveUsageHandler.CalculateMoveDamage(_currentTurn.move,_victim);
        var healAmount = _victim.pokemon.hp-damage<=0 ? _victim.pokemon.hp : damage; 
        healAmount *= healthDrainInfo.percentageOfDamage/100f;
        
        _moveUsageHandler.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:damage);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);

        if (_attacker.pokemon.hp >= _attacker.pokemon.maxHp)
        {
            moveDelay = false;
            yield break;
        }
        
        _moveUsageHandler.HealthGainDisplay(healAmount,healthGainer:_attacker);
        _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonDisplayName+" gained health");
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingHealthGain);
        moveDelay = false;
    }

    private IEnumerator ApplyDamageProtection()
    {
        if(_attacker.previousMove.move.moveName == _currentTurn.move.moveName)
        {
            int chance = 100;
            for (int i = 0; i < _attacker.previousMove.numRepetitions; i++)
                chance /= 2;
            if (Utility.RandomRange(1, 101) <= chance)
                _attacker.canBeDamaged = false;
            else
            {
                _attacker.canBeDamaged = true;
                _dialogueHandler.DisplayBattleInfo("It failed!");
            }
        }
        else
            _attacker.canBeDamaged = false;
        yield return null;
        moveDelay = false;
    }

    private IEnumerator CreateBarriers()
    {
        var barrierName = _currentTurn.move.moveName;
        if (_battleHandler.isDoubleBattle)
        {
            var currentAttacker = _battleHandler.battleParticipants[_currentTurn.attackerIndex]; 
            
            if (!_moveUsageHandler.HasDuplicateBarrier(currentAttacker, barrierName, true))
            {
                var newBarrier = new Barrier(barrierName, 0.33f, 5);
                
                currentAttacker.barriers.Add(newBarrier); 
                
                var partner= _battleHandler
                    .battleParticipants[currentAttacker.GetPartnerIndex()];

                if (partner.isActive)
                {
                    var barrierCopy = new Barrier(newBarrier.barrierName, newBarrier.barrierEffect, newBarrier.barrierDuration);
                    partner.barriers.Add(barrierCopy);
                }
                
                _dialogueHandler.DisplayBattleInfo(barrierName + " has been activated");
                yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
            }
        }
        else
        {
            var currentParticipant = _battleHandler.battleParticipants[_currentTurn.attackerIndex];

            if (_moveUsageHandler.HasDuplicateBarrier(currentParticipant, barrierName,true))
                yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
            else
            {
                currentParticipant.barriers.Add(new(barrierName,0.33f,5));
                
                _dialogueHandler.DisplayBattleInfo(barrierName + " has been activated");
            }
        }
        
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        
        moveDelay = false;
    }
    
    private IEnumerator OnFieldDamageModLogic()
    {
        var damageModifierInfo = _currentTurn.move.GetModule<DamageModifierInfo>();
        _dialogueHandler.DisplayBattleInfo(damageModifierInfo.damageChangeMessage);
        if (_moveUsageHandler.DamageModifierPresent(damageModifierInfo.typeAffected))
        {
            moveDelay = false;
            yield break;
        } 
        var damageModifier = new OnFieldDamageModifier(_battleHandler,_moveUsageHandler,_turnBasedCombatHandler,damageModifierInfo,_attacker);
        
        _battleHandler.OnParticipantFainted += RemoveOnFaint;
                
        void RemoveOnFaint(Battle_Participant faintedParticipant)
        {
            if (faintedParticipant != _attacker) return;
            _battleHandler.OnParticipantFainted -= RemoveOnFaint;
            damageModifier.RemoveOnSwitchOut(_attacker);
        }
        
        _battleHandler.OnSwitchOut += damageModifier.RemoveOnSwitchOut;
        _moveUsageHandler.AddFieldDamageModifier(damageModifier);
        moveDelay = false;
    }
    private IEnumerator IdentifyTarget()
    {
        if (_victim.immunityNegations.Any(n=> 
                n.moveName==ImmunityNegationMove.Foresight))
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            moveDelay = false;
            yield break;
        }
        _dialogueHandler.DisplayBattleInfo(_victim.pokemon.pokemonDisplayName +" was identified!");
        _victim.pokemon.buffAndDebuffs
            .RemoveAll(b => b.stat == Stat.Evasion);
        _victim.pokemon.evasion = 100;
        if(_victim.pokemon.HasType(PokemonType.Ghost))
        {
            var newImmunityNegation = new TypeImmunityNegation(_battleHandler,ImmunityNegationMove.Foresight
                , _attacker, _victim);

            newImmunityNegation.ImmunityNegationTypes.Add(PokemonType.Fighting);
            newImmunityNegation.ImmunityNegationTypes.Add(PokemonType.Normal);
            
            _battleHandler.OnParticipantFainted += RemoveOnFaint;
                
            void RemoveOnFaint(Battle_Participant faintedParticipant)
            {
                if (faintedParticipant != _attacker) return;
                _battleHandler.OnParticipantFainted -= RemoveOnFaint;
                newImmunityNegation.RemoveNegationOnSwitchOut(_attacker);
            }

            _battleHandler.OnSwitchOut += newImmunityNegation.RemoveNegationOnSwitchOut;
            _victim.immunityNegations.Add(newImmunityNegation);
        }
        moveDelay = false;
    }
    private IEnumerator ExecuteSemiInvulnerableMove()
    {
        if (_attacker.semiInvulnerabilityData.executionTurn)
        {
            _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonDisplayName
                                                        + _attacker.semiInvulnerabilityData.onHitMessage);
            _moveUsageHandler.DisplayDamage(_victim);
            _attacker.semiInvulnerabilityData.executionTurn = false;
            moveDelay = false;
            yield break;
        }

        var semiInvulnerableData = _currentTurn.move.GetModule<SemiInvulnerabilityInfo>();
        
        _attacker.semiInvulnerabilityData.displayMessage = semiInvulnerableData.displayMessage;
        _attacker.semiInvulnerabilityData.onHitMessage = semiInvulnerableData.onHitMessage;
        _attacker.semiInvulnerabilityData.turnData = new Turn(_currentTurn);

        _attacker.semiInvulnerabilityData.semiInvulnerabilities
            .AddRange(semiInvulnerableData.semiInvulnerabilities);

        _attacker.isSemiInvulnerable = true;
        _currentTurn.move.isSureHit = false;
        _attacker.semiInvulnerabilityData.executionTurn = true;
        _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonDisplayName+semiInvulnerableData.executionMessage);
        moveDelay = false;
    }
    
    IEnumerator ChangeWeather()
    {
        var weatherInfo =_currentTurn.move.GetModule<ChangeWeatherInfo>();;
        var newWeather = new WeatherCondition(weatherInfo.newWeatherCondition);
        _turnBasedCombatHandler.ChangeWeather(newWeather);
        yield return null;
        moveDelay = false;
    }
    private IEnumerator HealFromWeather()
    {
        if (_attacker.pokemon.hp >= _attacker.pokemon.maxHp)
        {
            _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonDisplayName+"'s health is already full!");
            moveDelay = false;
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
        int healthGain = Mathf.FloorToInt(_attacker.pokemon.maxHp * fraction);
        
        if (healthGain < 1 && _attacker.pokemon.hp < _attacker.pokemon.maxHp) healthGain = 1;
        
        _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonDisplayName+" restored it's health!");

        _moveUsageHandler.HealthGainDisplay(healthGain,healthGainer:_attacker);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingHealthGain);
        moveDelay = false;
    }


    public IEnumerator Pursuit(Battle_Participant pursuitUser,Battle_Participant switchOutVictim,Move pursuit)
    {
        _dialogueHandler.DisplayBattleInfo(pursuitUser.pokemon.pokemonDisplayName+" used "+pursuit.moveName
                                                    +" on "+switchOutVictim.pokemon.pokemonDisplayName+"!");
        _moveUsageHandler.attacker = pursuitUser;
        var pursuitDamage = _moveUsageHandler.CalculateMoveDamage(pursuit, switchOutVictim) * 2;
        
        _moveUsageHandler.DisplayDamage(switchOutVictim,displayEffectiveness:false,
            isSpecificDamage:true,predefinedDamage:pursuitDamage);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);      
    }

}
