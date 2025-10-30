using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class MoveLogicHandler : MonoBehaviour
{
    public static MoveLogicHandler Instance;
    private Turn _currentTurn;
    [SerializeField]private Battle_Participant _attacker;
    [SerializeField]private Battle_Participant _victim;
    public bool moveDelay;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public IEnumerator DetermineMoveLogic(Battle_Participant attacker, Battle_Participant victim, Turn currentTurn)
    {
        _attacker = attacker;
        _victim = victim;
        _currentTurn = currentTurn;
        switch (currentTurn.move.effectType)
        {
            case Move.EffectType.MultiTargetDamage:
               yield return HandleMultiTargetDamage(); 
               break;
            case Move.EffectType.Consecutive:
                yield return ExecuteConsecutiveMove(); 
                break;
            case Move.EffectType.HealthDrain:
                yield return DrainHealth(); 
                break;
            case Move.EffectType.DamageProtection:
                yield return ApplyDamageProtection(); 
                break;
            case Move.EffectType.WeatherHealthGain:
                yield return HealFromWeather(); 
                break;
            case Move.EffectType.IdentifyTarget:
                yield return IdentifyTarget(); 
                break;
            case Move.EffectType.BarrierCreation:
                yield return CreateBarriers(); 
                break;
            case Move.EffectType.OnFieldDamageModifier:
                yield return OnFieldDamageModLogic(); 
                break;
            case Move.EffectType.SemiInvulnerable:
                yield return ExecuteSemiInvulnerableMove(); 
                break;
            case Move.EffectType.WeatherChange:
                yield return ChangeWeather(); 
                break;
            case Move.EffectType.UniqueLogic:
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
        var allParticipants = Battle_handler.Instance.battleParticipants.ToList();
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
                Dialogue_handler.Instance.DisplayBattleInfo(_victim.pokemon.pokemonName+" protected itself");
                break;
            }
            if (_victim.pokemon.hp <= 0) break;
            
            Dialogue_handler.Instance.DisplayBattleInfo("Hit "+(i+1)+"!");//remove later if added animations
            Move_handler.Instance.DisplayDamage(_victim,false);
            yield return new WaitUntil(() => !Move_handler.Instance.displayingDamage);
            numHits++;
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        if (numHits>0 && consecutiveMoveInfo.displayHitCount && _victim.pokemon.hp > 0)
        {
            Move_handler.Instance.DisplayEffectiveness
                (BattleOperations.GetTypeEffectiveness(_victim, _currentTurn.move.type), _victim);
            Dialogue_handler.Instance.DisplayBattleInfo("It hit (x" + numHits + ") times");
        }
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        moveDelay = false;
    } 
    IEnumerator ApplyMultiTargetDamage(List<Battle_Participant> targets)
    {
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        foreach (var enemy in targets)
        {
            if (!enemy.isActive) continue;
            Move_handler.Instance.DisplayDamage(enemy);
            yield return new WaitUntil(() => !Move_handler.Instance.displayingDamage);
            yield return new WaitUntil(() => !Turn_Based_Combat.Instance.faintEventDelay && Battle_handler.Instance.faintQueue.Count == 0);
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        yield return new WaitUntil(() => Battle_handler.Instance.faintQueue.Count == 0 && !Turn_Based_Combat.Instance.faintEventDelay);
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        moveDelay = false;
    }
    IEnumerator HandleMultiTargetDamage()
    {
        var multiTargetInfo = _currentTurn.move.GetModule<MultiTargetDamageInfo>();
        var targets = new List<Battle_Participant>();
        switch (multiTargetInfo.target)
        {
            case MultiTargetDamageInfo.Target.AllEnemies :
                targets = _attacker.currentEnemies;
                break;
            case MultiTargetDamageInfo.Target.AllExceptSelf :
                targets = TargetAllExceptSelf();
                break;
        }
        yield return ApplyMultiTargetDamage(targets);
    }

    IEnumerator DrainHealth()
    {
        var healthDrainInfo = _currentTurn.move.GetModule<HealthDrainMoveInfo>();
        var damage = Move_handler.Instance.CalculateMoveDamage(_currentTurn.move,_victim);
        var healAmount = _victim.pokemon.hp-damage<=0 ? _victim.pokemon.hp : damage; 
        healAmount *= healthDrainInfo.percentageOfDamage/100f;
        
        Move_handler.Instance.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:damage);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingDamage);
        Move_handler.Instance.HealthGainDisplay(healAmount,healthGainer:_attacker);
        
        Dialogue_handler.Instance.DisplayBattleInfo(_attacker.pokemon.pokemonName+" gained health");
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingHealthGain);
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
                Dialogue_handler.Instance.DisplayBattleInfo("It failed!");
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
        if (Battle_handler.Instance.isDoubleBattle)
        {
            var currentParticipant = Battle_handler.Instance.battleParticipants[_currentTurn.attackerIndex];
            
            if (!Move_handler.Instance.HasDuplicateBarrier(currentParticipant, barrierName, true))
            {
                var newBarrier = new Barrier(barrierName, 0.33f, 5);
                
                currentParticipant.barriers.Add(newBarrier); 
                
                var partner= Battle_handler.Instance
                    .battleParticipants[currentParticipant.GetPartnerIndex()];

                if (partner.isActive)
                {
                    var barrierCopy = new Barrier(newBarrier.barrierName, newBarrier.barrierEffect, newBarrier.barrierDuration);
                    partner.barriers.Add(barrierCopy);
                }
                
                Dialogue_handler.Instance.DisplayBattleInfo(barrierName + " has been activated");
                yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
            }
        }
        else
        {
            var currentParticipant = Battle_handler.Instance.battleParticipants[_currentTurn.attackerIndex];

            if (Move_handler.Instance.HasDuplicateBarrier(currentParticipant, barrierName,true))
                yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
            else
            {
                currentParticipant.barriers.Add(new(barrierName,0.33f,5));
                
                Dialogue_handler.Instance.DisplayBattleInfo(barrierName + " has been activated");
            }
        }
        
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        
        moveDelay = false;
    }
    
    private IEnumerator OnFieldDamageModLogic()
    {
        var damageModifierInfo = _currentTurn.move.GetModule<DamageModifierInfo>();
        Dialogue_handler.Instance.DisplayBattleInfo(damageModifierInfo.damageChangeMessage);
        if (Move_handler.Instance.DamageModifierPresent(damageModifierInfo.typeAffected))
        {
            moveDelay = false;
            yield break;
        } 
        var damageModifier = new OnFieldDamageModifier(damageModifierInfo,_attacker);
        _attacker.OnPokemonFainted += ()=> damageModifier.RemoveOnSwitchOut(_attacker);
        Battle_handler.Instance.OnSwitchOut += damageModifier.RemoveOnSwitchOut;
        Move_handler.Instance.AddFieldDamageModifier(damageModifier);
        moveDelay = false;
    }
    private IEnumerator IdentifyTarget()
    {
        if (_victim.immunityNegations.Any(n=> 
                n.moveName==TypeImmunityNegation.ImmunityNegationMove.Foresight))
        {
            Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
            moveDelay = false;
            yield break;
        }
        Dialogue_handler.Instance.DisplayBattleInfo(_victim.pokemon.pokemonName +" was identified!");
        _victim.pokemon.buffAndDebuffs
            .RemoveAll(b => b.stat == PokemonOperations.Stat.Evasion);
        _victim.pokemon.evasion = 100;
        if(_victim.pokemon.HasType(PokemonOperations.Types.Ghost))
        {
            var newImmunityNegation = new TypeImmunityNegation(TypeImmunityNegation.ImmunityNegationMove.Foresight
                , _attacker, _victim);

            newImmunityNegation.ImmunityNegationTypes.Add(PokemonOperations.Types.Fighting);
            newImmunityNegation.ImmunityNegationTypes.Add(PokemonOperations.Types.Normal);
            _attacker.OnPokemonFainted += () => newImmunityNegation.RemoveNegationOnSwitchOut(_attacker);
            Battle_handler.Instance.OnSwitchOut += newImmunityNegation.RemoveNegationOnSwitchOut;
            _victim.immunityNegations.Add(newImmunityNegation);
        }
        moveDelay = false;
    }
    private IEnumerator ExecuteSemiInvulnerableMove()
    {
        if (_attacker.semiInvulnerabilityData.executionTurn)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(_attacker.pokemon.pokemonName
                                                        + _attacker.semiInvulnerabilityData.onHitMessage);
            Move_handler.Instance.DisplayDamage(_victim);
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
        Dialogue_handler.Instance.DisplayBattleInfo(_attacker.pokemon.pokemonName+semiInvulnerableData.executionMessage);
        moveDelay = false;
    }
    
    IEnumerator ChangeWeather()
    {
        var weatherInfo =_currentTurn.move.GetModule<ChangeWeatherInfo>();;
        var newWeather = new WeatherCondition(weatherInfo.newWeatherCondition);
        Turn_Based_Combat.Instance.ChangeWeather(newWeather);
        yield return null;
        moveDelay = false;
    }
    private IEnumerator HealFromWeather()
    {
        if (_attacker.pokemon.hp >= _attacker.pokemon.maxHp)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(_attacker.pokemon.pokemonName+"'s health is already full!");
            yield break;
        }
        float fraction;
        var currentWeather = Turn_Based_Combat.Instance.currentWeather.weather;
        
        switch (currentWeather)
        {
            case WeatherCondition.Weather.Sunlight:
                fraction = 2f / 3f;  
                break;
            case WeatherCondition.Weather.Rain:
            case WeatherCondition.Weather.Hail:
            case WeatherCondition.Weather.Sandstorm:
                fraction = 1f / 4f;          
                break;
            default: 
                fraction = 1f / 2f; 
                break;
        }
        int healthGain = Mathf.FloorToInt(_attacker.pokemon.maxHp * fraction);
        
        if (healthGain < 1 && _attacker.pokemon.hp < _attacker.pokemon.maxHp) healthGain = 1;
        
        Dialogue_handler.Instance.DisplayBattleInfo(_attacker.pokemon.pokemonName+" restored it's health!");

        Move_handler.Instance.HealthGainDisplay(healthGain,healthGainer:_attacker);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingHealthGain);
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
                Dialogue_handler.Instance.DisplayBattleInfo(_attacker.pokemon.pokemonName+" shattered "+barrier.barrierName);
                duplicateBarriers.Add(barrier.barrierName);
            }
            enemy.barriers.Clear();
        }
        
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        Move_handler.Instance.DisplayDamage(_victim);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingDamage);
        moveDelay = false;
    }
    
    void haze()
    {
        var validParticipants = Battle_handler.Instance.GetValidParticipants();
        foreach (var participant in validParticipants)
        {
            participant.pokemon.buffAndDebuffs.Clear();
            participant.statData.LoadActualStats();
        }
        moveDelay = false;
    }

    void hyperbeam()
    {
        Move_handler.Instance.DisplayDamage(_victim);
        var cancelledTurn = new Turn(_currentTurn);
        cancelledTurn.isCancelled = true;
        _attacker.currentCoolDown.UpdateCoolDown( 1,cancelledTurn,message: " must recharge!");
        moveDelay = false;
    }

    void bide()
    {
        if (_attacker.currentCoolDown.ExecuteTurn)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(_attacker.pokemon.pokemonName+" unleashed the power");
            if (_attacker.currentCoolDown.turnData.move.moveDamage > 0)
            {
                _currentTurn.move.moveDamage = _attacker.currentCoolDown.turnData.move.moveDamage;
                var typelessDamage = Move_handler.Instance.CalculateMoveDamage(_currentTurn.move, _victim, true);
                Move_handler.Instance.DisplayDamage(_victim,displayEffectiveness:false,isSpecificDamage:true
                    ,predefinedDamage:typelessDamage);
            }
            Move_handler.Instance.OnDamageDeal -= _attacker.currentCoolDown.StoreDamage;
            _attacker.currentCoolDown.ResetState();
        }
        else
        {
            Dialogue_handler.Instance.DisplayBattleInfo(_attacker.pokemon.pokemonName + " is storing power");
            var numTurns = Utility.RandomRange(2, 3);
            _attacker.currentCoolDown.UpdateCoolDown(numTurns,_currentTurn, " is storing power");
            Move_handler.Instance.OnDamageDeal += _attacker.currentCoolDown.StoreDamage;
        }
        moveDelay = false;
    }

    void sonicboom()
    {
        var sonicBoomDamage = 20f;
        Move_handler.Instance.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:sonicBoomDamage);
        moveDelay = false;
    }

    public IEnumerator Pursuit(Battle_Participant pursuitUser,Battle_Participant switchOutVictim,Move pursuit)
    {
        Dialogue_handler.Instance.DisplayBattleInfo(pursuitUser.pokemon.pokemonName+" used "+pursuit.moveName
                                                    +" on "+switchOutVictim.pokemon.pokemonName+"!");
        Move_handler.Instance.attacker = pursuitUser;
        var pursuitDamage = Move_handler.Instance.CalculateMoveDamage(pursuit, switchOutVictim) * 2;
        
        Move_handler.Instance.DisplayDamage(switchOutVictim,displayEffectiveness:false,
            isSpecificDamage:true,predefinedDamage:pursuitDamage);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingDamage);
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);      
    }
    void takedown()
    {
        StartCoroutine(RecoilDamageHandle());
    }
    private IEnumerator RecoilDamageHandle()
    {
        var damage = Move_handler.Instance.CalculateMoveDamage(_currentTurn.move, _victim);
        var recoilDamage = math.floor(damage / 4f);
        Move_handler.Instance.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:damage);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingDamage);
        Dialogue_handler.Instance.DisplayBattleInfo(_attacker.pokemon.pokemonName +" was hurt by the recoil");
        Move_handler.Instance.DisplayDamage(_attacker,isSpecificDamage:true
            ,predefinedDamage:recoilDamage,displayEffectiveness: false);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingDamage);
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
        Dialogue_handler.Instance.DisplayBattleInfo("Magnitude level "+magnitudeStrength);
        _currentTurn.move.moveDamage = baseDamage;
        StartCoroutine(ApplyMultiTargetDamage(TargetAllExceptSelf()));
    }

    void endeavor()
    {
        if (_victim.pokemon.hp < _attacker.pokemon.hp)
        {
            Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
            return;
        }
        var damage = _victim.pokemon.hp - _attacker.pokemon.hp;
        Move_handler.Instance.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:damage);
        moveDelay = false;
    }

    void furycutter()
    {
        var damageLevel = new[] { 10f, 20f, 40f, 80f, 160f };
        if (_attacker.previousMove.move.moveName == NameDB.GetMoveName(NameDB.LearnSetMove.FuryCutter))
        {
            _currentTurn.move.moveDamage = _attacker.previousMove.numRepetitions > 4?
                damageLevel[^1] : damageLevel[_attacker.previousMove.numRepetitions];
        }
        else
            _currentTurn.move.moveDamage = damageLevel[0];
        Move_handler.Instance.DisplayDamage(_victim);
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
            if(!Battle_handler.Instance.isTrainerBattle)
                battleEnded = true;
        }
        _victim.OnPokemonFainted += CancelOnBattleEnd;
        
        Move_handler.Instance.DisplayDamage(_victim);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingDamage);
        
        if(battleEnded) yield break;
        if (Utility.RandomRange(0, 101) > 10)
        {
            moveDelay = false;
            yield break;
        }
        
        //get buffs
        var allBuffs = new[]
        {
            PokemonOperations.Stat.Attack, PokemonOperations.Stat.Defense, 
            PokemonOperations.Stat.SpecialAttack, PokemonOperations.Stat.SpecialDefense,
            PokemonOperations.Stat.Speed
        };
        
        var waiting = true;
        void AwaitBuffAddition()
        {
            BattleOperations.OnBuffApplied -= AwaitBuffAddition;
            waiting = false;
        } 
        
        foreach (var buff in allBuffs)
        {
            waiting = true;
            BattleOperations.OnBuffApplied += AwaitBuffAddition;
            var buffData = new BuffDebuffData(_attacker, buff, true, 1);
            Move_handler.Instance.SelectRelevantBuffOrDebuff(buffData);
            yield return new WaitUntil(() => !waiting);
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
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
        Move_handler.Instance.DisplayDamage(_victim);
        moveDelay = false;
    }

    void falseswipe()
    {
        var damage = Move_handler.Instance.CalculateMoveDamage(_currentTurn.move, _victim);
        if (_victim.pokemon.hp - damage <= 0)
        {
            damage = _victim.pokemon.hp - 1;
        }
        Move_handler.Instance.DisplayDamage(_victim,isSpecificDamage:true,predefinedDamage:damage);
        moveDelay = false;
    }

    void bellydrum()
    {
        if (_attacker.pokemon.hp < 2)
        {
            Dialogue_handler.Instance.DisplayBattleInfo("But it failed!");
            moveDelay = false;
            return;
        }
        
        var selfDamage = math.floor(_attacker.pokemon.hp / 2f);
        Move_handler.Instance.DisplayDamage(_attacker,displayEffectiveness:false,
            isSpecificDamage:true,predefinedDamage:selfDamage);
        
        var buffData = new BuffDebuffData(_attacker, PokemonOperations.Stat.Attack, true, 6);
        Move_handler.Instance.SelectRelevantBuffOrDebuff(buffData);
    }

    void covet()
    {
        Move_handler.Instance.DisplayDamage(_victim);
        if (_victim.pokemon.hasItem && !_attacker.pokemon.hasItem)
        {
            if (_victim.pokemon.heldItem.itemType == Item_handler.ItemType.Berry)
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
                Dialogue_handler.Instance.DisplayBattleInfo("But it failed!");
                moveDelay = false;
                return;
            }
            Move_handler.Instance.repeatingMoveCycle = true;
            _currentTurn.move = _victim.previousMove.move;
            moveDelay = false;
            Dialogue_handler.Instance.DisplayBattleInfo(
                Turn_Based_Combat.Instance.GetMoveUsageText(_currentTurn.move,_attacker, _victim));
            Move_handler.Instance.OnMoveComplete += ()=> Move_handler.Instance.ExecuteMove(_currentTurn);
        }
        else
        {
            Dialogue_handler.Instance.DisplayBattleInfo("But it failed!");
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
            Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
            moveDelay = false;
            yield break;
        }
        if (!Battle_handler.Instance.isTrainerBattle)
        {
            moveDelay = false;
            Wild_pkm.Instance.inBattle = false;
            Battle_handler.Instance.EndBattle(false,true);
            Move_handler.Instance.doingMove = false;
            yield break;
        }
        if (_victim.isPlayer)
        {
            var living = Pokemon_party.Instance.GetLivingPokemon();
            if (living.Count < 2)
            {
                Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
                moveDelay = false;
                yield break;
            }
            
            //exclude current participants
            var excludedIndexes = 1;

            if (Battle_handler.Instance.isDoubleBattle)
                excludedIndexes++;
            
            var randomIndexOfLiving = Utility
                .RandomRange(excludedIndexes, living.Count);
            
            var pokemonAtIndex = Array.IndexOf(Pokemon_party.Instance.party,living[randomIndexOfLiving]);
            var switchData = new SwitchOutData(_currentTurn.victimIndex,pokemonAtIndex,_victim);
            
            yield return Turn_Based_Combat.Instance.HandleSwap(switchData,true);
        }
        else
        {
            var enemyTrainer = _victim.pokemonTrainerAI;
            var living = enemyTrainer.GetLivingPokemon();
            if (living.Count < 2)
            {
                Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
                moveDelay = false;
                yield break;
            }
            
            //exclude current participants
            var excludedIndexes = 1;

            if (Battle_handler.Instance.isDoubleBattle)
                excludedIndexes++;
            
            var randomIndexOfLiving = Utility
                .RandomRange(excludedIndexes, living.Count);
            
            var pokemonAtIndex = enemyTrainer.trainerParty.IndexOf(living[randomIndexOfLiving]);
            
            var switchData = new SwitchOutData(_currentTurn.victimIndex,pokemonAtIndex,_victim);
            yield return Turn_Based_Combat.Instance.HandleSwap(switchData,true);
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
        Dialogue_handler.Instance.DisplayBattleInfo(_attacker.pokemon.pokemonName+" fell asleep!");
        yield return new WaitForSeconds(1f);
        Move_handler.Instance.HealthGainDisplay(healthGain,healthGainer:_attacker);
        yield return new WaitUntil(() => !Move_handler.Instance.displayingHealthGain);
        _attacker.statusHandler.RemoveStatusEffect(true);
        yield return new WaitUntil(()=>_attacker.pokemon.statusEffect == PokemonOperations.StatusEffect.None);
        Move_handler.Instance.ApplyStatusToVictim(_attacker, PokemonOperations.StatusEffect.Sleep, 2);
        yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
        moveDelay = false;
    }
}
