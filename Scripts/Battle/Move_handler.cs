using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class Move_handler:MonoBehaviour
{
    public bool doingMove = false;
    public static Move_handler Instance;
    private Turn _currentTurn;
    [SerializeField]private Battle_Participant attacker;
    [SerializeField]private Battle_Participant victim;
    private readonly float[] _statLevels = {0.25f,0.29f,0.33f,0.4f,0.5f,0.67f,1f,1.5f,2f,2.5f,3f,3.5f,4f};
    private readonly float[] _accuracyAndEvasionLevels = {0.33f,0.375f,0.43f,0.5f,0.6f,0.75f,1f,1.33f,1.67f,2f,2.33f,2.67f,3f};
    private readonly float[] _critLevels = {6.25f,12.5f,25f,50f};
    private Battle_event[] _dialougeOrder={null,null,null,null,null};
    private bool _moveDelay = false;
    private bool _cancelMove = false;
    public bool processingOrder = false;
    private bool displayingHealthDecrease;
    public event Action OnMoveEnd;
    public event Func<Battle_Participant,Battle_Participant,Move,float,float> OnDamageDeal;
    public event Action<Battle_Participant,Move> OnMoveHit;
    public event Action<Battle_Participant,PokemonOperations.StatusEffect> OnStatusEffectHit;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void ExecuteMove(Turn turn)
    {
        _currentTurn = turn;
        attacker = Battle_handler.Instance.battleParticipants[turn.attackerIndex];
        victim = Battle_handler.Instance.battleParticipants[turn.victimIndex];
        if (_currentTurn.move.isMultiTarget || _currentTurn.move.isSelfTargeted)
            Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" used "+_currentTurn.move.moveName+"!");
        else
            Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" used "+_currentTurn.move.moveName+" on "+victim.pokemon.pokemonName+"!");
        StartCoroutine(MoveSequence());
    }
    void SetMoveSequence()
    {
        _dialougeOrder[0] = new Battle_event(DealDamage, _currentTurn.move.moveDamage > 0);
        _dialougeOrder[1] = new Battle_event(CheckVictimVulnerabilityToStatus, _currentTurn.move.hasStatus);
        _dialougeOrder[2] = new Battle_event(CheckBuffOrDebuffApplicability, _currentTurn.move.isBuffOrDebuff);
        _dialougeOrder[3] = new Battle_event(FlinchEnemy, _currentTurn.move.canCauseFlinch);
        _dialougeOrder[4] = new Battle_event(ConfuseEnemy,_currentTurn.move.canCauseConfusion);
    }
    private IEnumerator MoveSequence()
    {
        var moveEffectiveness = BattleOperations.GetTypeEffectiveness(victim, _currentTurn.move.type);
        if (moveEffectiveness == 0 && !_currentTurn.move.isMultiTarget)
            Dialogue_handler.Instance.DisplayBattleInfo("It doesn't affect "+victim.pokemon.pokemonName);
        else
        {
            SetMoveSequence();
            if (_currentTurn.move.hasSpecialEffect)
            {
                ExecuteMoveWithSpecialEffect();
            }
            else
            {
                victim.OnPokemonFainted += CancelMoveSequence;//victim faints after damage so the rest of move effect is ignored
                foreach (var battleEvent in _dialougeOrder)
                {
                    if (_cancelMove)
                        break;
                    yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
                    
                    if (!battleEvent.Condition) continue;
                    processingOrder = true;
                    battleEvent.Execute();
                    yield return new WaitUntil(() => !displayingHealthDecrease);
                    yield return new WaitUntil(() => !processingOrder);
                    yield return new WaitUntil(() => !Turn_Based_Combat.Instance.levelEventDelay);
                    yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
                } 
                victim.OnPokemonFainted -= CancelMoveSequence;
            }
        }
        yield return new WaitUntil(() => !_moveDelay);
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        ResetMoveUsage();
    }

    private void CancelMoveSequence()
    {
        _cancelMove = true;
    }
    void ExecuteMoveWithSpecialEffect()
    {
        if (!_currentTurn.move.hasSpecialEffect) return;
        _moveDelay = true;
        if (!_currentTurn.move.isConsecutive)
            Invoke(_currentTurn.move.moveName.Replace(" ", "").ToLower(), 0f);
        else
            ExecuteRandomConsecutiveMove();
    }

    public void ConfusionDamage(Battle_Participant confusionVictim)
    {
        var simulatedMove = ScriptableObject.CreateInstance<Move>();
        simulatedMove.isSpecial = false;
        simulatedMove.moveDamage = 40;
        simulatedMove.description = "Confusion";
        _currentTurn = new(simulatedMove, 0, 0, 0, 0);
        attacker = confusionVictim;
        victim = confusionVictim;
        StartCoroutine(DamageDisplay(confusionVictim,false));
    }
    private bool IsInvincible(Move move,Battle_Participant currentVictim)
    {
        if (currentVictim.pokemon.canBeDamaged || move.moveDamage == 0) return false;
        Dialogue_handler.Instance.DisplayBattleInfo(currentVictim.pokemon.pokemonName+" protected itself");
        
        if (!_currentTurn.move.isMultiTarget) CancelMoveSequence();
        return true;
    }
    private int CheckIfCrit()
    {
        var buffedCritRateIndex = Array.IndexOf(_critLevels, attacker.pokemon.critChance)
                                  + _currentTurn.move.critModifierIndex;
        float critChance = _critLevels[buffedCritRateIndex];
        if (UnityEngine.Random.Range(0f, 100f) >= critChance) return 1;
        return 2;
    }

    private float CalculateMoveDamage(Move move,Battle_Participant currentVictim)
    { 
        var isConfusionDamage = move.description == "Confusion"; //this is a cop out,but easiest option
        if (IsInvincible(move, currentVictim)) return 0;
        var critValue = CheckIfCrit();
        if(critValue>1 && !isConfusionDamage) Dialogue_handler.Instance.DisplayBattleInfo("Critical Hit!");
        float damageDealt = 0;
        var levelFactor = ((attacker.pokemon.currentLevel * 2) / 5f) + 2;
        var stab = 1f;
        float attackTypeValue = 0;
        float attackDefenseRatio = 0;
        var randomFactor = (float)Utility.RandomRange(85, 101) / 100;
        var typeEffectiveness = isConfusionDamage? 1: BattleOperations.GetTypeEffectiveness(currentVictim, move.type);
        attackTypeValue = move.isSpecial? attacker.pokemon.specialAttack : attacker.pokemon.attack;
        attackDefenseRatio = SetAtkDefRatio(critValue,move.isSpecial,attacker,currentVictim);
        if (BattleOperations.is_Stab(attacker.pokemon, move.type))
            stab = 1.5f;
        float baseDamage = (levelFactor * move.moveDamage *
                             (attackTypeValue/ move.moveDamage))/attacker.pokemon.currentLevel;
        float damageModifier = critValue*stab*randomFactor*typeEffectiveness;
        damageDealt = math.trunc(damageModifier * baseDamage * attackDefenseRatio);
        if (isConfusionDamage) return damageDealt;
        float damageAfterBuff = OnDamageDeal?.Invoke(attacker,victim,move,damageDealt) ?? damageDealt; 
        OnMoveHit?.Invoke(attacker,move);
        damageAfterBuff = AccountForVictimsBarriers(move,currentVictim,damageAfterBuff);
        return damageAfterBuff;
    }

    private float AccountForVictimsBarriers(Move move,Battle_Participant currentVictim,float damage)
    {
        foreach (var barrier in currentVictim.Barrieirs)
        {
            if ((move.isSpecial && barrier.barrierName == "Light Screen")
                || (!move.isSpecial && barrier.barrierName == "Reflect"))
                return  damage-(damage*barrier.barrierEffect);
        }
        return damage;
    }
    private void DisplayEffectiveness(float typeEffectiveness,Battle_Participant currentVictim)
    {
        if ((int)math.trunc(typeEffectiveness) == 1) return;
        var message = "";
        if (typeEffectiveness == 0)
            message= "It does'nt affect "+currentVictim.pokemon.pokemonName+"!";
        else
            message=(typeEffectiveness > 1)?"It's Super effective!":"It's not very effective!";
        Dialogue_handler.Instance.DisplayBattleInfo(message);
    }
    private float SetAtkDefRatio(int crit, bool isSpecial, Battle_Participant currentAttacker, Battle_Participant currentVictim)
    {
        float atk, def;
        bool canIgnoreStages = currentAttacker.pokemon.currentLevel >= currentVictim.pokemon.currentLevel  && crit == 2;
        if (!isSpecial)
        {
            atk = canIgnoreStages && currentAttacker.statData.attack < currentAttacker.pokemon.attack
                ? currentAttacker.pokemon.attack  // Ignore debuff
                : currentAttacker.statData.attack;
            
            def = canIgnoreStages && currentVictim.statData.defense > currentVictim.pokemon.defense
                ? currentVictim.pokemon.defense  // Ignore buff
                : currentVictim.statData.defense;
        }
        else
        {
            atk = canIgnoreStages && currentAttacker.statData.spAtk < currentAttacker.pokemon.specialAttack
                ? currentAttacker.pokemon.specialAttack
                : currentAttacker.statData.spAtk;
            
            def = canIgnoreStages && currentVictim.statData.spDef > currentVictim.pokemon.specialDefense
                ? currentVictim.pokemon.specialDefense
                : currentVictim.statData.spDef;
        }
        return atk / def;
    }
    private IEnumerator DamageDisplay(Battle_Participant currentVictim,bool displayEffectiveness = true)
    {
        displayingHealthDecrease = true;
        var damage = CalculateMoveDamage(_currentTurn.move, currentVictim);
        
        var healthAfterDecrease = Mathf.Clamp(currentVictim.pokemon.hp - damage,0,currentVictim.pokemon.hp);
        
        float displayHp = currentVictim.pokemon.hp;
         
        while (displayHp > healthAfterDecrease)
        {
            float newHp = Mathf.MoveTowards(displayHp, healthAfterDecrease,
                (0.25f/currentVictim.pokemon.healthPhase) * Time.deltaTime);
            displayHp = Mathf.Floor(newHp);
            currentVictim.pokemon.hp = displayHp;
            
            yield return null;
        }
        
        currentVictim.pokemon.hp = healthAfterDecrease;
        yield return new WaitUntil(() =>  currentVictim.pokemon.hp <= 0 || currentVictim.pokemon.hp<=healthAfterDecrease);
        var typeEffectiveness = BattleOperations.GetTypeEffectiveness(currentVictim, _currentTurn.move.type);

        if (!_currentTurn.move.isConsecutive && displayEffectiveness)
        {
            DisplayEffectiveness(typeEffectiveness,currentVictim);
        }
        currentVictim.pokemon.TakeDamage(0);
        displayingHealthDecrease = false;
    }
    private void DealDamage()
    {
        if (_currentTurn.move.hasSpecialEffect)
        { processingOrder = false; return; }
        StartCoroutine(DamageDisplay(victim));
        processingOrder = false;
    } 
    private void ResetMoveUsage()
    {
        OnMoveEnd?.Invoke();
        doingMove = false;
        _cancelMove = false;
    }

    private void CheckVictimVulnerabilityToStatus()
    {
        if (_currentTurn.move.hasSpecialEffect || !_currentTurn.move.hasStatus)
        { processingOrder = false; return; }
        if (victim.pokemon.statusEffect != PokemonOperations.StatusEffect.None)
        { 
            if(_currentTurn.move.statusEffect==victim.pokemon.statusEffect)
                Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName+" already has a "+victim.pokemon.statusEffect+" effect!");
            processingOrder = false;
            return;
        }
        if (victim.pokemon.hp <= 0){processingOrder = false; return;}
        if (!victim.pokemon.canBeDamaged)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName+" protected itself");
            processingOrder = false;return;
        }
        if (Utility.RandomRange(1, 101) < _currentTurn.move.statusChance)
            if(_currentTurn.move.isMultiTarget)
                foreach (Battle_Participant enemy in attacker.currentEnemies)
                    HandleStatusApplication(enemy,_currentTurn.move,true);
            else{
                HandleStatusApplication(victim,_currentTurn.move,true);
            }
        processingOrder = false;
    }
    bool CheckInvalidStatusEffect(PokemonOperations.StatusEffect status,string typeName,Move move)
    {
        string[] invalidCombinations = {
            "poisonpoison","badlypoisonpoison", "burnfire", "paralysiselectric", "freezeice" };
        foreach(string s in invalidCombinations)
            if ((status + typeName).ToLower() == s)
            {
                if(move.moveDamage==0)//if its only a status causing move
                    Dialogue_handler.Instance.DisplayBattleInfo("It failed");
                return true;
            }
        return false;
    }
    public void HandleStatusApplication(Battle_Participant currentVictim,Move move, bool displayMessage)
    {
        foreach (var type in currentVictim.pokemon.types)
            if(CheckInvalidStatusEffect(move.statusEffect, type.typeName,move))return;
        OnStatusEffectHit?.Invoke(currentVictim,move.statusEffect);
        if (displayMessage)
            Dialogue_handler.Instance.DisplayBattleInfo($"{currentVictim.pokemon.pokemonName} {GetStatusMessage(move.statusEffect)}");
        ApplyStatusToVictim(currentVictim,move.statusEffect);
    }
    public static string GetStatusMessage(PokemonOperations.StatusEffect status)
    {
         var displayMessage = status switch
            {
                PokemonOperations.StatusEffect.Paralysis=>"was paralyzed",
                PokemonOperations.StatusEffect.Burn=>"was burned",
                PokemonOperations.StatusEffect.BadlyPoison=>"was badly poisoned",
                PokemonOperations.StatusEffect.Poison=>"was poisoned",
                PokemonOperations.StatusEffect.Sleep=>"fell fast asleep",
                PokemonOperations.StatusEffect.Freeze=>"was frozen",
                _=>""
            };
        return displayMessage;
    }
    public void ApplyStatusToVictim(Battle_Participant participant,PokemonOperations.StatusEffect status)
    {
        participant.pokemon.statusEffect = status;
        var numTurnsOfStatus = (status==PokemonOperations.StatusEffect.Sleep)? Utility.RandomRange(1, 5) : 0;
        participant.statusHandler.GetStatusEffect(numTurnsOfStatus);
    }

    public void ApplyStatDropImmunity(Battle_Participant participant,int numTurns)
    {
        if (!participant.isActive) return;
        participant.statusHandler.GetStatDropImmunity(numTurns);
    }

    void ConfuseEnemy()
    {
        if (!victim.pokemon.canBeDamaged)
        {
            processingOrder = false;
            return;
        }
        if (Utility.RandomRange(1, 101) < _currentTurn.move.statusChance)
        {
            var randomNum = Utility.RandomRange(1, 6);
            victim.statusHandler.GetConfusion(randomNum);
        }
        processingOrder = false;
    }
    void FlinchEnemy()
    {
        if (!victim.pokemon.canBeDamaged || !victim.pokemon.canBeFlinched)
        {
            processingOrder = false;
            return;
        }
        if (Utility.RandomRange(1, 101) < _currentTurn.move.statusChance)
        {
            victim.pokemon.canAttack = false;
            victim.pokemon.isFlinched = true;
        }
        processingOrder = false;
    }
    void CheckBuffOrDebuffApplicability()
    {
        if (!_currentTurn.move.isBuffOrDebuff) { processingOrder = false;return;}
        if (Utility.RandomRange(1, 101) > _currentTurn.move.buffOrDebuffChance)
        { processingOrder = false; return;}
        //allows the display of buff message, must be here in case silent buff happened
        BattleOperations.CanDisplayDialougue = true; 
        var buffData = _currentTurn.move.buffOrDebuffData;
        if (!_currentTurn.move.isSelfTargeted)
        {//affecting enemy
            if ( (_currentTurn.move.isMultiTarget && !Battle_handler.Instance.isDoubleBattle) 
                || !_currentTurn.move.isMultiTarget)
            {
                if (!victim.pokemon.canBeDamaged || victim.pokemon.immuneToStatReduction)
                {
                    Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName + " protected itself");
                }
                else
                {
                    var data = new BuffDebuffData(victim, buffData.stat, buffData.isIncreasing, buffData.amount);
                    SelectRelevantBuffOrDebuff(data);
                }
            } 
            if(_currentTurn.move.isMultiTarget && Battle_handler.Instance.isDoubleBattle)
                StartCoroutine(MultiTargetBuff_Debuff(buffData.stat, buffData.isIncreasing, buffData.amount));
        }
        else//affecting attacker
        {
            var data = new BuffDebuffData(attacker, buffData.stat, buffData.isIncreasing, buffData.amount);
            SelectRelevantBuffOrDebuff(data);
        }
        processingOrder = false;
    }

    IEnumerator MultiTargetBuff_Debuff(PokemonOperations.Stat stat, bool isIncreasing,int buffAmount)
    {
        foreach (Battle_Participant enemy in new List<Battle_Participant>(attacker.currentEnemies) )
        {
            if (enemy.pokemon.canBeDamaged && !enemy.pokemon.immuneToStatReduction)
            {
                var data = new BuffDebuffData(enemy, stat, isIncreasing,buffAmount);
                SelectRelevantBuffOrDebuff(data);
            }
            else
                Dialogue_handler.Instance.DisplayBattleInfo(enemy.pokemon.pokemonName + " protected itself");
            yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
        }
    }
    public void SelectRelevantBuffOrDebuff(BuffDebuffData data)
    {
        var unModifiedStats = data.Receiver.statData;
        var affectedPokemon = data.Receiver.pokemon;
        switch (data.Stat)
        {
            case PokemonOperations.Stat.Defense:
                affectedPokemon.defense = GetUpdatedStat(unModifiedStats.defense,data) ?? affectedPokemon.defense;
                break;
            case PokemonOperations.Stat.Attack:
                affectedPokemon.attack = GetUpdatedStat(unModifiedStats.attack,data) ?? affectedPokemon.attack;
                break;
            case PokemonOperations.Stat.SpecialDefense:
                affectedPokemon.specialDefense = GetUpdatedStat(unModifiedStats.spDef,data) ?? affectedPokemon.specialDefense;
                break;
            case PokemonOperations.Stat.SpecialAttack:
                affectedPokemon.specialAttack = GetUpdatedStat(unModifiedStats.spAtk,data) ?? affectedPokemon.specialAttack;
                break;
            case PokemonOperations.Stat.Speed:
                affectedPokemon.speed = GetUpdatedStat(unModifiedStats.speed,data) ?? affectedPokemon.speed;
                break;
            case PokemonOperations.Stat.Accuracy:
                affectedPokemon.accuracy = GetUpdatedStat(unModifiedStats.accuracy,data) ?? affectedPokemon.accuracy;
                break;
            case PokemonOperations.Stat.Evasion:
                affectedPokemon.evasion = GetUpdatedStat(unModifiedStats.evasion,data) ?? affectedPokemon.evasion;
                break;
            case PokemonOperations.Stat.Crit:
                affectedPokemon.critChance = GetUpdatedStat(unModifiedStats.crit,data)?? affectedPokemon.critChance;
                break; 
        }
    }
    private float? GetUpdatedStat(float unmodifiedStatValue, BuffDebuffData data)
    {
        BattleOperations.ChangeOrCreateBuffOrDebuff(data);
        var buff = BattleOperations.SearchForBuffOrDebuff(data.Receiver.pokemon, data.Stat) 
                   ?? new Buff_Debuff(PokemonOperations.Stat.None,0,true); // if null return same value
        if (buff.isAtLimit) return null;
        switch (data.Stat)
        {
            case PokemonOperations.Stat.Accuracy:
            case PokemonOperations.Stat.Evasion:
                return math.trunc(unmodifiedStatValue * _accuracyAndEvasionLevels[buff.stage+6]);
            case PokemonOperations.Stat.Crit:
                return _critLevels[buff.stage];
            default:
                return math.trunc(unmodifiedStatValue * _statLevels[buff.stage+6]); 
        }
    }
    List<Battle_Participant> TargetAllExceptSelf()
    {
        var allParticipants = Battle_handler.Instance.battleParticipants.ToList();
        allParticipants.RemoveAll(p => !p.isActive);
        allParticipants.RemoveAll(p => p.pokemon.pokemonID == attacker.pokemon.pokemonID);
        return allParticipants;
    }
    IEnumerator ExecuteConsecutiveMove(int numRepetitions,bool displayMessage = true)
    {
        var numHits = 0;
        for (int i = 0; i < numRepetitions; i++)
        {
            if (!victim.pokemon.canBeDamaged) break;
            
            if (victim.pokemon.hp <= 0) break;
            
            if (!Turn_Based_Combat.Instance.MoveSuccessful(_currentTurn)) break; //if miss
            
            Dialogue_handler.Instance.DisplayBattleInfo("Hit "+(i+1)+"!");//remove later if added animations
            StartCoroutine(DamageDisplay(victim));
            yield return new WaitUntil(() => !displayingHealthDecrease);
            numHits++;
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        if (numHits>0 && displayMessage)
        {
            DisplayEffectiveness(BattleOperations.GetTypeEffectiveness(victim, _currentTurn.move.type), victim);
            Dialogue_handler.Instance.DisplayBattleInfo("It hit (x" + numHits + ") times");
        }
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        _moveDelay = false;
    } 
    private void ExecuteRandomConsecutiveMove()
    {
        var numRepetitions = Utility.RandomRange(1, 6);
        StartCoroutine(ExecuteConsecutiveMove(numRepetitions));
    } 
    IEnumerator ApplyMultiTargetDamage(List<Battle_Participant> targets)
    {
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        foreach (var enemy in targets)
        {
            StartCoroutine(DamageDisplay(enemy));
            yield return new WaitUntil(() => !displayingHealthDecrease);
            yield return new WaitUntil(() => !Turn_Based_Combat.Instance.faintEventDelay && Battle_handler.Instance.faintQueue.Count == 0);
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        yield return new WaitUntil(() => Battle_handler.Instance.faintQueue.Count == 0 && !Turn_Based_Combat.Instance.faintEventDelay);
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        _moveDelay = false;

    }
    void doublekick()
    {
        StartCoroutine(ExecuteConsecutiveMove(2,false));
    }
    void surf()
    {
        StartCoroutine(ApplyMultiTargetDamage(attacker.currentEnemies));
    }
    void earthquake()
    {
        StartCoroutine(ApplyMultiTargetDamage(TargetAllExceptSelf()));
    }
    void absorb()
    {
        var damage = CalculateMoveDamage(_currentTurn.move,victim);
        var healAmount = damage/ 2f;
        StartCoroutine(DamageDisplay(victim));
        attacker.pokemon.hp =( (healAmount+attacker.pokemon.hp) < attacker.pokemon.maxHp)? 
            math.trunc(math.abs(healAmount)) : attacker.pokemon.maxHp;
        Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" gained health");
        _moveDelay = false;
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
    void protect()
    {
        if(attacker.previousMove.move.moveName == "Protect")
        {
            int chance = 100;
            for (int i = 0; i < attacker.previousMove.numRepetitions; i++)
                chance /= 2;
            if (Utility.RandomRange(1, 101) <= chance) //success
                attacker.pokemon.canBeDamaged = false;
            else
            {
                attacker.pokemon.canBeDamaged = true;
                Dialogue_handler.Instance.DisplayBattleInfo("It failed!");
            }
        }
        else//success
            attacker.pokemon.canBeDamaged = false;
        _moveDelay = false;
    }
    void brickbreak()
    {
        StartCoroutine(ShatterBarriers());
    }
    private IEnumerator ShatterBarriers()
    {
        var duplicateBarriers = new List<string>();
        foreach (var enemy in attacker.currentEnemies)
        {
            if(!enemy.isActive)continue;
            foreach (var barrier in enemy.Barrieirs)
            {
                if (duplicateBarriers.Contains(barrier.barrierName)) continue;
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" shattered "+barrier.barrierName);
                duplicateBarriers.Add(barrier.barrierName);
            }
            enemy.Barrieirs.Clear();
        }
        
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        
        StartCoroutine(DamageDisplay(victim));
        yield return new WaitUntil(() => !displayingHealthDecrease);
        _moveDelay = false;
    }

    private IEnumerator CreateBarriers(string barrierName)
    {
        if (Battle_handler.Instance.isDoubleBattle)
        {
            var currentParticipant = Battle_handler.Instance.battleParticipants[_currentTurn.attackerIndex];
            
            if (!HasDuplicateBarrier(currentParticipant, barrierName, true))
            {
                var newBarrier = new Barrier(barrierName, 0.33f, 5);
                
                currentParticipant.Barrieirs.Add(newBarrier); 
                
                var partner= Battle_handler.Instance
                    .battleParticipants[currentParticipant.GetPartnerIndex()];

                if (partner.isActive)
                {
                    var barrierCopy = new Barrier(newBarrier.barrierName, newBarrier.barrierEffect, newBarrier.barrierDuration);
                    partner.Barrieirs.Add(barrierCopy);
                }
                
                Dialogue_handler.Instance.DisplayBattleInfo(barrierName + " has been activated");
                yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
            }
        }
        else
        {
            var currentParticipant = Battle_handler.Instance.battleParticipants[_currentTurn.attackerIndex];

            if (HasDuplicateBarrier(currentParticipant, barrierName,true))
                yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
            else
            {
                currentParticipant.Barrieirs.Add(new(barrierName,0.33f,5));
                
                Dialogue_handler.Instance.DisplayBattleInfo(barrierName + " has been activated");
            }
        }
        
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        
        _moveDelay = false;
    }

    public bool HasDuplicateBarrier(Battle_Participant currentParticipant,string  barrierName,bool displayMessage)
    {
        var duplicateBarrier = currentParticipant.Barrieirs.Any(b => b.barrierName == barrierName); 

        if (Battle_handler.Instance.isDoubleBattle)
        {
            var partner= Battle_handler.Instance
                .battleParticipants[currentParticipant.GetPartnerIndex()];
                
            if(partner.isActive)
                if(partner.Barrieirs.Any(b => b.barrierName == barrierName))
                {
                    duplicateBarrier = true;
                }
        }

        if (duplicateBarrier && displayMessage)
            Dialogue_handler.Instance.DisplayBattleInfo(barrierName + " is already activated");
        
        return duplicateBarrier;
    }
    void reflect()
    {
        StartCoroutine(CreateBarriers("Reflect"));
    }

    void lightscreen()
    {
        StartCoroutine(CreateBarriers("Light Screen"));
    }
}