using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class MoveLogicHandler : MonoBehaviour,IInjectable
{
    private Turn _currentTurn;
    [SerializeField]private Battle_Participant _attacker;
    [SerializeField]private Battle_Participant _victim;
    public bool moveDelay;
    
    private Dialogue_handler _dialogueHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Pokemon_party _pokemonPartyHandler;
    private Wild_pkm _wildPokemonHandler;
    private BattleVisuals _battleVisualsHandler;
    private Battle_handler _battleHandler;
    private Move_handler _moveUsageHandler;
    private BattleOperations _battleOperationsHandler;
    
    public void Inject(ServiceContainer container)
    {
        _battleOperationsHandler = container.Resolve<BattleOperations>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _battleHandler = container.Resolve<Battle_handler>();
        _wildPokemonHandler = container.Resolve<Wild_pkm>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _moveUsageHandler = container.Resolve<Move_handler>();
        gameObject.SetActive(true);
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
        Invoke(_currentTurn.move.moveName.Replace(" ","").ToLower(),0);
        yield return new WaitUntil(() => !moveDelay);
    }
    List<Battle_Participant> TargetAllExceptSelf()
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
                _dialogueHandler.DisplayBattleInfo(_victim.pokemon.pokemonName+" protected itself");
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
                (BattleOperations.CheckTypeEffectiveness(_victim, _currentTurn.move.type), _victim);
            _dialogueHandler.DisplayBattleInfo("It hit (x" + numHits + ") times");
        }
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        moveDelay = false;
    } 
    IEnumerator ApplyMultiTargetDamage(List<Battle_Participant> targets)
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
        _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName+" gained health");
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
            var currentParticipant = _battleHandler.battleParticipants[_currentTurn.attackerIndex];
            
            if (!_moveUsageHandler.HasDuplicateBarrier(currentParticipant, barrierName, true))
            {
                var newBarrier = new Barrier(barrierName, 0.33f, 5);
                
                currentParticipant.barriers.Add(newBarrier); 
                
                var partner= _battleHandler
                    .battleParticipants[currentParticipant.GetPartnerIndex()];

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
        _attacker.OnPokemonFainted += ()=> damageModifier.RemoveOnSwitchOut(_attacker);
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
        _dialogueHandler.DisplayBattleInfo(_victim.pokemon.pokemonName +" was identified!");
        _victim.pokemon.buffAndDebuffs
            .RemoveAll(b => b.stat == Stat.Evasion);
        _victim.pokemon.evasion = 100;
        if(_victim.pokemon.HasType(Types.Ghost))
        {
            var newImmunityNegation = new TypeImmunityNegation(_battleHandler,ImmunityNegationMove.Foresight
                , _attacker, _victim);

            newImmunityNegation.ImmunityNegationTypes.Add(Types.Fighting);
            newImmunityNegation.ImmunityNegationTypes.Add(Types.Normal);
            _attacker.OnPokemonFainted += () => newImmunityNegation.RemoveNegationOnSwitchOut(_attacker);
            _battleHandler.OnSwitchOut += newImmunityNegation.RemoveNegationOnSwitchOut;
            _victim.immunityNegations.Add(newImmunityNegation);
        }
        moveDelay = false;
    }
    private IEnumerator ExecuteSemiInvulnerableMove()
    {
        if (_attacker.semiInvulnerabilityData.executionTurn)
        {
            _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName
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
        _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName+semiInvulnerableData.executionMessage);
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
            _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName+"'s health is already full!");
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
        
        _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName+" restored it's health!");

        _moveUsageHandler.HealthGainDisplay(healthGain,healthGainer:_attacker);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingHealthGain);
        moveDelay = false;
    }
    
    void brickbreak()
    {
        StartCoroutine(ShatterBarriers());
    }
    private IEnumerator ShatterBarriers()
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
        moveDelay = false;
    }
    
    void haze()
    {
        var validParticipants = _battleHandler.GetValidParticipants();
        foreach (var participant in validParticipants)
        {
            participant.pokemon.buffAndDebuffs.Clear();
            participant.statData.LoadActualStats();
        }
        moveDelay = false;
    }

    void hyperbeam()
    {
        _moveUsageHandler.DisplayDamage(_victim);
        var cancelledTurn = new Turn(_currentTurn);
        cancelledTurn.isCancelled = true;
        _attacker.currentCoolDown.UpdateCoolDown( 1,cancelledTurn,message: " must recharge!");
        moveDelay = false;
    }

    void bide()
    {
        if (_attacker.currentCoolDown.ExecuteTurn)
        {
            _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName+" unleashed the power");
            if (_attacker.currentCoolDown.turnData.move.moveDamage > 0)
            {
                _currentTurn.move.moveDamage = _attacker.currentCoolDown.turnData.move.moveDamage;
                var typelessDamage = _moveUsageHandler.CalculateMoveDamage(_currentTurn.move, _victim, true);
                _moveUsageHandler.DisplayDamage(_victim,displayEffectiveness:false,isSpecificDamage:true
                    ,predefinedDamage:typelessDamage);
            }
            _moveUsageHandler.OnDamageDeal -= _attacker.currentCoolDown.StoreDamage;
            _attacker.currentCoolDown.ResetState();
        }
        else
        {
            _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName + " is storing power");
            var numTurns = Utility.RandomRange(2, 3);
            _attacker.currentCoolDown.UpdateCoolDown(numTurns,_currentTurn, " is storing power");
            _moveUsageHandler.OnDamageDeal += _attacker.currentCoolDown.StoreDamage;
        }
        moveDelay = false;
    }

    void sonicboom()
    {
        var sonicBoomDamage = 20f;
        _moveUsageHandler.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:sonicBoomDamage);
        moveDelay = false;
    }

    public IEnumerator Pursuit(Battle_Participant pursuitUser,Battle_Participant switchOutVictim,Move pursuit)
    {
        _dialogueHandler.DisplayBattleInfo(pursuitUser.pokemon.pokemonName+" used "+pursuit.moveName
                                                    +" on "+switchOutVictim.pokemon.pokemonName+"!");
        _moveUsageHandler.attacker = pursuitUser;
        var pursuitDamage = _moveUsageHandler.CalculateMoveDamage(pursuit, switchOutVictim) * 2;
        
        _moveUsageHandler.DisplayDamage(switchOutVictim,displayEffectiveness:false,
            isSpecificDamage:true,predefinedDamage:pursuitDamage);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);      
    }
    void takedown()
    {
        StartCoroutine(RecoilDamageHandle());
    }
    private IEnumerator RecoilDamageHandle()
    {
        var damage = _moveUsageHandler.CalculateMoveDamage(_currentTurn.move, _victim);
        var recoilDamage = math.floor(damage / 4f);
        _moveUsageHandler.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:damage);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
        _dialogueHandler.DisplayBattleInfo(_attacker.pokemon.pokemonName +" was hurt by the recoil");
        _moveUsageHandler.DisplayDamage(_attacker,isSpecificDamage:true
            ,predefinedDamage:recoilDamage,displayEffectiveness: false);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
        moveDelay = false;
    }

    void magnitude()
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
        StartCoroutine(ApplyMultiTargetDamage(TargetAllExceptSelf()));
    }

    void endeavor()
    {
        if (_victim.pokemon.hp < _attacker.pokemon.hp)
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            return;
        }
        var damage = _victim.pokemon.hp - _attacker.pokemon.hp;
        _moveUsageHandler.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:damage);
        moveDelay = false;
    }

    void furycutter()
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
        moveDelay = false;
    }
    void silverwind()
    {
        StartCoroutine(HandleSilverwind());
    }
    private IEnumerator HandleSilverwind()
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
            moveDelay = false;
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
            _moveUsageHandler.SelectRelevantBuffOrDebuff(buffData);
            yield return new WaitUntil(() => !waiting);
        }
        
        statChangeMessage = BattleOperations.GetBuffResultMessage(true,_attacker.pokemon,allBuffs);
        _battleVisualsHandler.OnStatVisualDisplayed += AwaitBuffVisual;
        waiting = true;
        _battleVisualsHandler.SelectStatChangeVisuals(Stat.Multi,_attacker,statChangeMessage);
        yield return new WaitUntil(() => !waiting);
        moveDelay = false;
    }
    void flail()
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
        moveDelay = false;
    }

    void falseswipe()
    {
        var damage = _moveUsageHandler.CalculateMoveDamage(_currentTurn.move, _victim);
        if (_victim.pokemon.hp - damage <= 0)
        {
            damage = _victim.pokemon.hp - 1;
        }
        _moveUsageHandler.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:damage);
        moveDelay = false;
    }

    void bellydrum()
    {
        if (_attacker.pokemon.hp < 2)
        {
            _dialogueHandler.DisplayBattleInfo("But it failed!");
            moveDelay = false;
            return;
        }
        
        var selfDamage = math.floor(_attacker.pokemon.hp / 2f);
        _moveUsageHandler.DisplayDamage(_attacker,displayEffectiveness:false,
            isSpecificDamage:true,predefinedDamage:selfDamage);
        
        var buffData = new BuffDebuffData(_attacker, Stat.Attack, true, 6);
        _moveUsageHandler.SelectRelevantBuffOrDebuff(buffData);
    }

    void covet()
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
        moveDelay = false;
    }

    void mirrormove()
    {
        if (_victim.previousMove is {failedAttempt:false})
        {
            var nonCopyableMoves = new[] {"Detect","Protect","Haze"};
            if (_victim.previousMove.move.isSelfTargeted
                || nonCopyableMoves.Contains(_victim.previousMove.move.moveName))
            {
                _dialogueHandler.DisplayBattleInfo("But it failed!");
                moveDelay = false;
                return;
            }
            _moveUsageHandler.repeatingMoveCycle = true;
            _currentTurn.move = _victim.previousMove.move;
            moveDelay = false;
            _dialogueHandler.DisplayBattleInfo(
                _turnBasedCombatHandler.GetMoveUsageText(_currentTurn.move,_attacker, _victim));
            _moveUsageHandler.OnMoveComplete += ()=> _moveUsageHandler.ExecuteMove(_currentTurn);
        }
        else
        {
            _dialogueHandler.DisplayBattleInfo("But it failed!");
            moveDelay = false;
        }
    }

    void whirlwind()
    {
        StartCoroutine(HandleWhirlwind());
    }

    private IEnumerator HandleWhirlwind()
    {
        if (_attacker.pokemon.currentLevel<_victim.pokemon.currentLevel)
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            moveDelay = false;
            yield break;
        }
        if (!_battleHandler.isTrainerBattle)
        {
            moveDelay = false;
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
                moveDelay = false;
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
                moveDelay = false;
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
        
        moveDelay = false;
    }
    void rest()
    {
        StartCoroutine(HandleRest());
    }

    IEnumerator HandleRest()
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
        moveDelay = false;
    }
}
