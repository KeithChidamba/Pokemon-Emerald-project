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
    private Battle_event[] _dialougeOrder={null,null,null,null,null,null,null};
    [SerializeField]private List<OnFieldDamageModifier> _onFieldDamageModifiers = new();
    [SerializeField]private List<DamageDisplayData> _damageDisplayQueue = new();
    [SerializeField]private List<DamageDisplayData> _healhGainQueue = new();
    private bool _repeatingMoveCycle = false;
    private bool _moveDelay = false;
    private bool _cancelMove = false;
    public bool processingOrder = false;
    public bool displayingDamage;
    public bool displayingHealthGain;
    public event Func<Battle_Participant,Battle_Participant,Move,float,float> OnDamageCalc;
    public event Action<float,Battle_Participant> OnDamageDeal;
    public event Action<Battle_Participant,Move> OnMoveHit;
    public event Action<Battle_Participant,PokemonOperations.StatusEffect> OnStatusEffectHit;
    private event Action OnMoveComplete;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Battle_handler.Instance.OnBattleEnd += ()=> _onFieldDamageModifiers.Clear();;
    }
    public void ExecuteMove(Turn turn)
    {
        OnMoveComplete = null;
        _currentTurn = turn;
        attacker = Battle_handler.Instance.battleParticipants[turn.attackerIndex];
        victim = Battle_handler.Instance.battleParticipants[turn.victimIndex];
        StartCoroutine(MoveSequence());
    }
    void SetMoveSequence()
    {
        _dialougeOrder[0] = new Battle_event(DealDamage, _currentTurn.move.moveDamage > 0);
        _dialougeOrder[1] = new Battle_event(CheckVictimVulnerabilityToStatus, _currentTurn.move.hasStatus);
        _dialougeOrder[2] = new Battle_event(CheckBuffOrDebuffApplicability, _currentTurn.move.isBuffOrDebuff);
        _dialougeOrder[3] = new Battle_event(FlinchEnemy, _currentTurn.move.canCauseFlinch);
        _dialougeOrder[4] = new Battle_event(ConfuseEnemy,_currentTurn.move.canCauseConfusion);
        _dialougeOrder[5] = new Battle_event(TrapEnemy,_currentTurn.move.canTrap);
        _dialougeOrder[6] = new Battle_event(InfatuateEnemy,_currentTurn.move.canInfatuate);
    }
    private IEnumerator MoveSequence()
    {
        var moveEffectiveness = BattleOperations.GetTypeEffectiveness(victim, _currentTurn.move.type);
        if (moveEffectiveness == 0 && !_currentTurn.move.isMultiTarget && !_currentTurn.move.hasTypelessEffect)
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
                    yield return new WaitUntil(() => !processingOrder);
                    yield return new WaitUntil(()=> !displayingDamage);
                    yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
                } 
                victim.OnPokemonFainted -= CancelMoveSequence;
            }
        }

        yield return new WaitUntil(() => !_moveDelay);
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        yield return new WaitUntil(()=> !displayingDamage);
        yield return new WaitUntil(()=> !displayingHealthGain);
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

    public IEnumerator DealConfusionDamage(Battle_Participant confusionVictim)
    {
        var confusionDamage = CalculateConfusionDamage(confusionVictim);
        
        DisplayDamage(confusionVictim,displayEffectiveness:false,isSpecificDamage:true
            ,predefinedDamage:confusionDamage);
        
        yield return new WaitUntil(()=> !displayingDamage);
    }
    private bool IsInvincible(Move move,Battle_Participant currentVictim)
    {
        if (currentVictim.canBeDamaged || move.moveDamage == 0) return false;
        Dialogue_handler.Instance.DisplayBattleInfo(currentVictim.pokemon.pokemonName+" protected itself");
        
        if (!_currentTurn.move.isMultiTarget) CancelMoveSequence();
        return true;
    }

    float CalculateConfusionDamage(Battle_Participant confusionVictim)
    {
        int level = confusionVictim.pokemon.currentLevel;
        float levelFactor = ((level * 2f) / 5f) + 2f;
        int power = 40;
        float attackDefenseRatio = confusionVictim.pokemon.attack 
                                   / Mathf.Max(1, confusionVictim.pokemon.defense);

        float randomFactor = Utility.RandomRange(217, 256) / 255f;
        
        float baseDamage = ((levelFactor * power * attackDefenseRatio) / 50f) + 2f;

        int damage = Mathf.FloorToInt(baseDamage * randomFactor);

        if (damage < 1) damage = 1;

        return damage;
    }
    private float CalculateMoveDamage(Move move,Battle_Participant currentVictim,bool isTypeless=false)
    { 
        if (IsInvincible(move, currentVictim)) return 0;
        
        //calc crit
        var critValue = 1;
        var buffedCritRateIndex = Array.IndexOf(_critLevels, attacker.pokemon.critChance)
                                  + move.critModifierIndex;
        float critChance = _critLevels[buffedCritRateIndex];
        if (UnityEngine.Random.Range(0f, 100f) < critChance)
            critValue =  2;
        
        if (critValue > 1f) Dialogue_handler.Instance.DisplayBattleInfo("Critical Hit!");
        
        float levelFactor = ((attacker.pokemon.currentLevel * 2f) / 5f) + 2f;
        
        float attackDefenseRatio = SetAtkDefRatio(critValue, move.isSpecial, attacker, currentVictim);

        float stab = BattleOperations.IsStab(attacker.pokemon, move.type) ? 1.5f : 1f;
        
        float typeEffectiveness = isTypeless? 1f 
            :BattleOperations.GetTypeEffectiveness(currentVictim, move.type);
        
        float randomFactor = Utility.RandomRange(217, 256) / 255f;

        float baseDamage = ((levelFactor * move.moveDamage * attackDefenseRatio) / 50f) + 2f;
                
        if(currentVictim.isSemiInvulnerable)
        { 
            var semiInvulnerability = currentVictim.semiInvulnerabilityData
                .semiInvulnerabilities.FirstOrDefault(s => s.moveName == move.moveName);
            baseDamage *= semiInvulnerability?.damageMultiplier ?? 1f;
        }
        
        float damageModifier = critValue * stab * typeEffectiveness * randomFactor;
        
        int damageDealt = Mathf.FloorToInt(baseDamage * damageModifier);
        
        if (damageDealt < 1) damageDealt = 1;
        
        float damageAfterAbilityBuff = OnDamageCalc?.Invoke(attacker,victim,move,damageDealt) ?? damageDealt;
        float damageAfterFieldModifiers = ApplyFieldDamageModifiers(damageAfterAbilityBuff,move.type);
        float finalDamage = AccountForVictimsBarriers(move,currentVictim,damageAfterFieldModifiers);
        OnDamageDeal?.Invoke(finalDamage,currentVictim);
        OnMoveHit?.Invoke(attacker,move);
        return finalDamage;
    }
    private float ApplyFieldDamageModifiers(float currentDamage, Type moveType)
    {
        foreach (var modifier in _onFieldDamageModifiers)
        {
            if (modifier.typeAffected.ToString() == moveType.typeName)
                return currentDamage * modifier.damageModifier;
        }
        return currentDamage;
    }
    private float AccountForVictimsBarriers(Move move,Battle_Participant currentVictim,float damage)
    {
        foreach (var barrier in currentVictim.barriers)
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
            message= "It doesn't affect "+currentVictim.pokemon.pokemonName+"!";
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
    public void HealthGainDisplay(float healthGained,Pokemon affectedPokemon = null,Battle_Participant healthGainer = null)
    {
        DamageDisplayData data = new DamageDisplayData
            (healthGainer,predefinedHealthChange:healthGained,affectedPokemon:affectedPokemon);
        _healhGainQueue.Add(data);
        if (!displayingHealthGain) StartCoroutine(ProcessHealthGainDisplay());
    }
    public void DisplayDamage(Battle_Participant currentVictim, bool displayEffectiveness = true,
        bool isSpecificDamage = false, float predefinedDamage = 0)
    {
        DamageDisplayData data = new DamageDisplayData(currentVictim,displayEffectiveness, isSpecificDamage, predefinedDamage);
        _damageDisplayQueue.Add(data);
        if (!displayingDamage) StartCoroutine(ProcessDamageDisplay());
    }
    IEnumerator ProcessHealthGainDisplay()
    {
        displayingHealthGain = true; 
        while (_healhGainQueue.Count > 0)
        {
            var data = _healhGainQueue[0];
            var healthAfterChange = Mathf.Clamp(data.affectedPokemon.hp 
                  + data.predefinedHealthChange,0,data.affectedPokemon.maxHp);
            float displayHp = data.affectedPokemon.hp;
            while (displayHp < healthAfterChange)
            {
                float newHp = Mathf.MoveTowards(displayHp, healthAfterChange
                    ,data.affectedPokemon.healthPhase  * 10f *Time.deltaTime);
                displayHp = newHp;
                data.affectedPokemon.hp =  Mathf.Floor(displayHp);
                
                if(InputStateHandler.Instance.currentState.stateGroups
                   .Contains(InputStateHandler.StateGroup.PokemonParty))
                {//update party health ui
                    data.affectedPokemon.ChangeHealth(null);
                }  
                
                yield return null;
            }
            yield return new WaitUntil(() => data.affectedPokemon.hp >= Mathf.Floor(healthAfterChange));
            _healhGainQueue.RemoveAt(0);
        }
        displayingHealthGain = false;
    }
    IEnumerator ProcessDamageDisplay()
    {
        displayingDamage = true; 
        while (_damageDisplayQueue.Count > 0)
        {
            var data = _damageDisplayQueue[0];
            var damage = data.isSpecificDamage? data.predefinedHealthChange 
                : CalculateMoveDamage(_currentTurn.move, data.affectedParticipant);
            
            if (damage == 0)
            {//protected enemy
                {
                    _damageDisplayQueue.RemoveAt(0);
                    continue;
                }
            }
            var healthAfterChange = Mathf
                .Clamp(data.affectedPokemon.hp - damage,0,data.affectedPokemon.maxHp);
            float displayHp = data.affectedPokemon.hp;
            while (displayHp > healthAfterChange)
            {
                float newHp = Mathf.MoveTowards(displayHp, healthAfterChange,
                    (20f/data.affectedPokemon.healthPhase) * Time.deltaTime);
                displayHp = newHp;
                data.affectedPokemon.hp =  Mathf.Floor(displayHp);
                yield return null;
            }
            yield return new WaitUntil(() => data.affectedPokemon.hp <= healthAfterChange);
            data.affectedPokemon.hp =  Mathf.Floor(healthAfterChange);
            if (data.displayEffectiveness)
            {
                if (!_currentTurn.move.isConsecutive)
                {
                    var typeEffectiveness = BattleOperations
                        .GetTypeEffectiveness(data.affectedParticipant, _currentTurn.move.type);
                    DisplayEffectiveness(typeEffectiveness,data.affectedParticipant);
                }
            }
            data.affectedPokemon.ChangeHealth(attacker);  
            _damageDisplayQueue.RemoveAt(0);
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
            yield return null;
        }
        displayingDamage = false;
    }
    private void DealDamage()
    {
        if (_currentTurn.move.hasSpecialEffect)
        { processingOrder = false; return; }
        DisplayDamage(victim);
        processingOrder = false;
    } 
    private void ResetMoveUsage()
    {
        OnMoveComplete?.Invoke();
        if (_repeatingMoveCycle)
        {
            _repeatingMoveCycle = false;
            return;
        }
        doingMove = false;
        _cancelMove = false;
    }

    private void CheckVictimVulnerabilityToStatus()
    {
        if (_currentTurn.move.hasSpecialEffect || !_currentTurn.move.hasStatus)
        {
            processingOrder = false; 
            return;
        }
        if (victim.pokemon.statusEffect != PokemonOperations.StatusEffect.None)
        {
            if (_currentTurn.move.statusEffect == victim.pokemon.statusEffect) 
                Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName+" already has a "+victim.pokemon.statusEffect+" effect!");
            else
                Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
            
            processingOrder = false;
            return;
        }
        if (victim.pokemon.hp <= 0){processingOrder = false; return;}
        if (!victim.canBeDamaged)
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
    private static string GetStatusMessage(PokemonOperations.StatusEffect status)
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
    public void ApplyStatusToVictim(Battle_Participant participant,PokemonOperations.StatusEffect status, int numTurns=0)
    {
        participant.pokemon.statusEffect = status;
        var numTurnsOfStatus = 0;
        if (numTurns != 0)
            participant.statusHandler.GetStatusEffect(numTurns);
        else
        {
            if(status==PokemonOperations.StatusEffect.Sleep)
                numTurnsOfStatus = Utility.RandomRange(1, 5);
        }
        participant.statusHandler.GetStatusEffect(numTurnsOfStatus);
    }

    public void ApplyStatChangeImmunity(Battle_Participant participant,StatChangeData.StatChangeability changeability,int numTurns)
    {
        if (!participant.isActive) return;
        participant.statusHandler.GetStatChangeImmunity(changeability,numTurns);
    }
    private void TrapEnemy()
    {
        ApplyTrap(victim, true);
    }
    public void ApplyTrap(Battle_Participant enemy, bool isMoveEffect)
    {
        if (enemy.pokemon.ability.abilityName == "Levitate")
        {
            Dialogue_handler.Instance.DisplayBattleInfo(enemy.pokemon.pokemonName+ "can't be trapped");
            processingOrder = false;
            return;
        }
       
        if(isMoveEffect)
        {
            if (enemy.pokemon.HasType(PokemonOperations.Types.Ghost)
                && !attacker.pokemon.HasType(PokemonOperations.Types.Ghost))
            {//only ghost can trap ghost with moves
                Dialogue_handler.Instance.DisplayBattleInfo(enemy.pokemon.pokemonName+ "can't be trapped");
                processingOrder = false;
                return;
            }

            if (!enemy.canEscape)
            {
                processingOrder = false;
                Dialogue_handler.Instance.DisplayBattleInfo(enemy.pokemon.pokemonName + " is already trapped");
                return;
            }
           
            if (_currentTurn.move.statusChance == 0)
                enemy.statusHandler.SetupTrapDuration(hasDuration:false);
            else
            {
                var numTurnsOfTrap = Utility.RandomRange(2, (int)_currentTurn.move.statusChance+1);
                enemy.statusHandler.SetupTrapDuration(numTurnsOfTrap,_currentTurn.move);
            }
            processingOrder = false;
            return;
        }
        enemy.statusHandler.SetupTrapDuration(hasDuration:false);
    }
    void ConfuseEnemy()
    {
        if (victim.isConfused)
        {
            processingOrder = false;
            return;
        }
        if (_currentTurn.move.isSelfTargeted)
            ApplyConfusion(attacker);
        else
        {
            if (victim.canBeDamaged)
                ApplyConfusion(victim);
            else
                Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName + " protected itself");
        }
        processingOrder = false;
    }

    void ApplyConfusion(Battle_Participant victimOfConfusion)
    {
        if (Utility.RandomRange(1, 101) <= _currentTurn.move.statusChance)
        {
            var randomNumTurns = Utility.RandomRange(1, 6);
            Dialogue_handler.Instance.DisplayBattleInfo(victimOfConfusion.pokemon.pokemonName
                                                        + " was confused");
            victimOfConfusion.statusHandler.GetConfusion(randomNumTurns);
        }
    }
    void FlinchEnemy()
    {
        if (!victim.canBeDamaged || !victim.pokemon.canBeFlinched)
        {
            processingOrder = false;
            return;
        }
        if (Utility.RandomRange(1, 101) < _currentTurn.move.statusChance)
        {
            victim.canAttack = false;
            victim.isFlinched = true;
        }
        processingOrder = false;
    }

    void InfatuateEnemy()
    {
        if (victim.isInfatuated)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName+" is already in love!");
            processingOrder = false;
            return;
        }
        if (!victim.canBeDamaged || !victim.pokemon.canBeInfatuated)
        {
            processingOrder = false;
            return;
        }
        if (victim.pokemon.gender == PokemonOperations.Gender.None 
            || attacker.pokemon.gender == PokemonOperations.Gender.None
            || attacker.pokemon.gender == victim.pokemon.gender)
        {
            Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
            processingOrder = false;
            return;
        }
        victim.isInfatuated = true;
    }
    void CheckBuffOrDebuffApplicability()
    {
        if (!_currentTurn.move.isBuffOrDebuff) { processingOrder = false;return;}
        if (Utility.RandomRange(1, 101) > _currentTurn.move.buffOrDebuffChance)
        { processingOrder = false; return;}
        //allows the display of buff message, must be here in case silent buff happened
        BattleOperations.CanDisplayDialougue = true; 
        
        foreach (var buffData in _currentTurn.move.buffOrDebuffData)
        {
            if (!_currentTurn.move.isSelfTargeted)
            {//affecting enemy
                if ( (_currentTurn.move.isMultiTarget && !Battle_handler.Instance.isDoubleBattle) 
                     || !_currentTurn.move.isMultiTarget)
                {
                    if (!victim.canBeDamaged || victim.ProtectedFromStatChange(buffData.isIncreasing))
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
        }
        processingOrder = false;
    }

    IEnumerator MultiTargetBuff_Debuff(PokemonOperations.Stat stat, bool isIncreasing,int buffAmount)
    {
        foreach (Battle_Participant enemy in new List<Battle_Participant>(attacker.currentEnemies) )
        {
            if (enemy.canBeDamaged && !victim.ProtectedFromStatChange(isIncreasing))
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
                   ?? new Buff_Debuff(PokemonOperations.Stat.None, 0, true); // if null return same value
        if (buff.isAtLimit) return null;
        return ModifyStatValue(data.Stat, unmodifiedStatValue, buff.stage);
    }

    public float ModifyStatValue(PokemonOperations.Stat stat, float unmodifiedStatValue ,int buffStage)
    {
        switch (stat)
        {
            case PokemonOperations.Stat.Accuracy:
            case PokemonOperations.Stat.Evasion:
                return math.trunc(unmodifiedStatValue * _accuracyAndEvasionLevels[buffStage+6]);
            case PokemonOperations.Stat.Crit:
                return _critLevels[buffStage];
            default:
                return math.trunc(unmodifiedStatValue * _statLevels[buffStage+6]); 
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
            if (!victim.canBeDamaged)
            {
                Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName+" protected itself");
                break;
            }
            if (victim.pokemon.hp <= 0) break;
            
            Dialogue_handler.Instance.DisplayBattleInfo("Hit "+(i+1)+"!");//remove later if added animations
            DisplayDamage(victim,false);
            yield return new WaitUntil(() => !displayingDamage);
            numHits++;
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        if (numHits>0 && displayMessage && victim.pokemon.hp > 0)
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
            if (!enemy.isActive) continue;
            DisplayDamage(enemy);
            yield return new WaitUntil(() => !displayingDamage);
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

    IEnumerator DrainHealth(float fractionOfDamage)
    {
        var damage = CalculateMoveDamage(_currentTurn.move,victim);
        var healAmount = victim.pokemon.hp-damage<0 ? victim.pokemon.hp : damage; 
        healAmount /= fractionOfDamage;
        DisplayDamage(victim,isSpecificDamage:true,predefinedDamage:damage);
        yield return new WaitUntil(() => !displayingDamage);
        HealthGainDisplay(healAmount,healthGainer:attacker);
        
        Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" gained health");
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        yield return new WaitUntil(() => !displayingHealthGain);
        _moveDelay = false;
    }

    private void leechlife()
    {
        StartCoroutine(DrainHealth(2f));
    }
    void absorb()
    {
        StartCoroutine(DrainHealth(2f));
    }
    void megadrain()
    {
        StartCoroutine(DrainHealth(2f));
    }
    void gigadrain()
    {
        StartCoroutine(DrainHealth(2f));
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

    void ApplyDamageProtection()
    {
        if(attacker.previousMove.move.moveName == _currentTurn.move.moveName)
        {
            int chance = 100;
            for (int i = 0; i < attacker.previousMove.numRepetitions; i++)
                chance /= 2;
            if (Utility.RandomRange(1, 101) <= chance)
                attacker.canBeDamaged = false;
            else
            {
                attacker.canBeDamaged = true;
                Dialogue_handler.Instance.DisplayBattleInfo("It failed!");
            }
        }
        else
            attacker.canBeDamaged = false;
        _moveDelay = false;
    }
    void protect()
    {
        ApplyDamageProtection();
    }

    void detect()
    {
        ApplyDamageProtection();
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
            foreach (var barrier in enemy.barriers)
            {
                if (duplicateBarriers.Contains(barrier.barrierName)) continue;
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" shattered "+barrier.barrierName);
                duplicateBarriers.Add(barrier.barrierName);
            }
            enemy.barriers.Clear();
        }
        
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        DisplayDamage(victim);
        yield return new WaitUntil(() => !displayingDamage);
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

            if (HasDuplicateBarrier(currentParticipant, barrierName,true))
                yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
            else
            {
                currentParticipant.barriers.Add(new(barrierName,0.33f,5));
                
                Dialogue_handler.Instance.DisplayBattleInfo(barrierName + " has been activated");
            }
        }
        
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        
        _moveDelay = false;
    }

    public bool HasDuplicateBarrier(Battle_Participant currentParticipant,string  barrierName,bool displayMessage)
    {
        var duplicateBarrier = currentParticipant.barriers.Any(b => b.barrierName == barrierName); 

        if (Battle_handler.Instance.isDoubleBattle)
        {
            var partner= Battle_handler.Instance
                .battleParticipants[currentParticipant.GetPartnerIndex()];
                
            if(partner.isActive)
                if(partner.barriers.Any(b => b.barrierName == barrierName))
                {
                    duplicateBarrier = true;
                }
        }

        if (duplicateBarrier && displayMessage)
            Dialogue_handler.Instance.DisplayBattleInfo(barrierName + " is already activated");
        
        return duplicateBarrier;
    }
    void reflect()
    {//add to name db
        StartCoroutine(CreateBarriers("Reflect"));
    }

    void lightscreen()
    {
        StartCoroutine(CreateBarriers("Light Screen"));
    }
    void haze()
    {
        var validParticipants = Battle_handler.Instance.GetValidParticipants();
        foreach (var participant in validParticipants)
        {
            participant.pokemon.buffAndDebuffs.Clear();
            participant.statData.LoadActualStats();
        }
        _moveDelay = false;
    }

    void hyperbeam()
    {
        DisplayDamage(victim);
        var cancelledTurn = new Turn(_currentTurn);
        cancelledTurn.isCancelled = true;
        attacker.currentCoolDown.UpdateCoolDown( 1,cancelledTurn,message: " must recharge!");
        _moveDelay = false;
    }

    void bide()
    {
        if (attacker.currentCoolDown.ExecuteTurn)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" unleashed the power");
            if (attacker.currentCoolDown.turnData.move.moveDamage > 0)
            {
                _currentTurn.move.moveDamage = attacker.currentCoolDown.turnData.move.moveDamage;
                var typelessDamage = CalculateMoveDamage(_currentTurn.move, victim, true);
                DisplayDamage(victim,displayEffectiveness:false,isSpecificDamage:true
                    ,predefinedDamage:typelessDamage);
            }
            OnDamageDeal -= attacker.currentCoolDown.StoreDamage;
            attacker.currentCoolDown.ResetState();
        }
        else
        {
            Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName + " is storing power");
            var numTurns = Utility.RandomRange(2, 3);
            attacker.currentCoolDown.UpdateCoolDown(numTurns,_currentTurn, " is storing power");
            OnDamageDeal += attacker.currentCoolDown.StoreDamage;
        }
        _moveDelay = false;
    }

    void sonicboom()
    {
        var sonicBoomDamage = 20f;
        DisplayDamage(victim,isSpecificDamage:true,predefinedDamage:sonicBoomDamage);
        _moveDelay = false;
    }

    public IEnumerator Pursuit(Battle_Participant pursuitUser,Battle_Participant switchOutVictim,Move pursuit)
    {
        Dialogue_handler.Instance.DisplayBattleInfo(pursuitUser.pokemon.pokemonName+" used "+pursuit.moveName
                                                    +" on "+switchOutVictim.pokemon.pokemonName+"!");
        attacker = pursuitUser;
        var pursuitDamage = CalculateMoveDamage(pursuit, switchOutVictim) * 2;
        
        DisplayDamage(switchOutVictim,displayEffectiveness:false,
            isSpecificDamage:true,predefinedDamage:pursuitDamage);
        yield return new WaitUntil(() => !displayingDamage);
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);      
    }
    public void AddFieldDamageModifier(OnFieldDamageModifier newModifier)
    {
        _onFieldDamageModifiers.Add(newModifier);
    }
    public void RemoveFieldDamageModifier(PokemonOperations.Types modifierTypeAffected)
    {
        _onFieldDamageModifiers.RemoveAll(m=>m.typeAffected==modifierTypeAffected);
    }

    void mudsport()
    {
        Dialogue_handler.Instance.DisplayBattleInfo("The power of electric type moves was weakened");
        if (_onFieldDamageModifiers.Any(m => 
                m.typeAffected == PokemonOperations.Types.Electric))
        {
            _moveDelay = false;
            return;
        }
        var mudSportModifier = new OnFieldDamageModifier(0.5f, PokemonOperations.Types.Electric,attacker);
        attacker.OnPokemonFainted += ()=> mudSportModifier.RemoveOnSwitchOut(attacker);
        Battle_handler.Instance.OnSwitchOut += mudSportModifier.RemoveOnSwitchOut;
        AddFieldDamageModifier(mudSportModifier);
        _moveDelay = false;
    }

    void takedown()
    {
        StartCoroutine(RecoilDamageHandle());
    }
    private IEnumerator RecoilDamageHandle()
    {
        var damage = CalculateMoveDamage(_currentTurn.move, victim);
        var recoilDamage = math.floor(damage / 4f);
        DisplayDamage(victim,isSpecificDamage:true,predefinedDamage:damage);
        yield return new WaitUntil(() => !displayingDamage);
        Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName +" was hurt by the recoil");
        DisplayDamage(attacker,isSpecificDamage:true
            ,predefinedDamage:recoilDamage,displayEffectiveness: false);
        yield return new WaitUntil(() => !displayingDamage);
        _moveDelay = false;
    }

    private void IdentifyTarget()
    {
        if (victim.immunityNegations.Any(n=> 
                n.moveName==TypeImmunityNegation.ImmunityNegationMove.Foresight))
        {
            Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
            _moveDelay = false;
            return;
        }
        Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName +" was identified!");
        victim.pokemon.buffAndDebuffs
            .RemoveAll(b => b.stat == PokemonOperations.Stat.Evasion);
        victim.pokemon.evasion = 100;
        if(victim.pokemon.HasType(PokemonOperations.Types.Ghost))
        {
            var newImmunityNegation = new TypeImmunityNegation(TypeImmunityNegation.ImmunityNegationMove.Foresight
                , attacker, victim);

            newImmunityNegation.ImmunityNegationTypes.Add(PokemonOperations.Types.Fighting);
            newImmunityNegation.ImmunityNegationTypes.Add(PokemonOperations.Types.Normal);
            attacker.OnPokemonFainted += () => newImmunityNegation.RemoveNegationOnSwitchOut(attacker);
            Battle_handler.Instance.OnSwitchOut += newImmunityNegation.RemoveNegationOnSwitchOut;
            victim.immunityNegations.Add(newImmunityNegation);
        }
        _moveDelay = false;
    }
    void foresight()
    {
        IdentifyTarget();
    }
    void odorsleuth()
    {
        IdentifyTarget();
    }
    void endeavor()
    {
        if (victim.pokemon.hp < attacker.pokemon.hp)
        {
            Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
            return;
        }
        var damage = victim.pokemon.hp - attacker.pokemon.hp;
        DisplayDamage(victim,isSpecificDamage:true,predefinedDamage:damage);
        _moveDelay = false;
    }

    void furycutter()
    {
        var damageLevel = new[] { 10f, 20f, 40f, 80f, 160f };
        if (attacker.previousMove.move.moveName == NameDB.GetMoveName(NameDB.LearnSetMove.FuryCutter))
        {
            _currentTurn.move.moveDamage = attacker.previousMove.numRepetitions > 4?
                damageLevel[^1] : damageLevel[attacker.previousMove.numRepetitions];
        }
        else
            _currentTurn.move.moveDamage = damageLevel[0];
        DisplayDamage(victim);
        _moveDelay = false;
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
        victim.OnPokemonFainted += CancelOnBattleEnd;
        
        DisplayDamage(victim);
        yield return new WaitUntil(() => !displayingDamage);
        
        if(battleEnded) yield break;
        if (Utility.RandomRange(0, 101) > 10)
        {
            _moveDelay = false;
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
            var buffData = new BuffDebuffData(attacker, buff, true, 1);
            SelectRelevantBuffOrDebuff(buffData);
            yield return new WaitUntil(() => !waiting);
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        _moveDelay = false;
    }
    void flail()
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
                _currentTurn.move.moveDamage = phase.damage;
                break;
            }
        }
        DisplayDamage(victim);
        _moveDelay = false;
    }

    void falseswipe()
    {
        var damage = CalculateMoveDamage(_currentTurn.move, victim);
        if (victim.pokemon.hp - damage <= 0)
        {
            damage = victim.pokemon.hp - 1;
        }
        DisplayDamage(victim,isSpecificDamage:true,predefinedDamage:damage);
        _moveDelay = false;
    }

    void bellydrum()
    {
        if (attacker.pokemon.hp < 2)
        {
            Dialogue_handler.Instance.DisplayBattleInfo("But it failed!");
            _moveDelay = false;
            return;
        }
        
        var selfDamage = math.floor(attacker.pokemon.hp / 2f);
        DisplayDamage(attacker,displayEffectiveness:false,
            isSpecificDamage:true,predefinedDamage:selfDamage);
        
        var buffData = new BuffDebuffData(attacker, PokemonOperations.Stat.Attack, true, 6);
        SelectRelevantBuffOrDebuff(buffData);
    }

    void covet()
    {
        DisplayDamage(victim);
        if (victim.pokemon.hasItem && !attacker.pokemon.hasItem)
        {
            if (victim.pokemon.heldItem.itemType == Item_handler.ItemType.Berry)
            {
                attacker.pokemon.GiveItem(Obj_Instance.CreateItem(victim.pokemon.heldItem));
                victim.pokemon.RemoveHeldItem();
            }
        }
        _moveDelay = false;
    }

    void mirrormove()
    {
        if (victim.previousMove!=null)
        {
            _repeatingMoveCycle = true;
            _currentTurn.move = victim.previousMove.move;
            _moveDelay = false;
            Dialogue_handler.Instance.DisplayBattleInfo(
                Turn_Based_Combat.Instance.GetMoveUsageText(_currentTurn.move,attacker, victim));
            OnMoveComplete += ()=> ExecuteMove(_currentTurn);
        }
        else
        {
            Dialogue_handler.Instance.DisplayBattleInfo("But it failed!");
            _moveDelay = false;
        }
    }

    void dig()
    {
        if (attacker.semiInvulnerabilityData.executionTurn)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName
                                                        + attacker.semiInvulnerabilityData.onHitMessage);
            DisplayDamage(victim);
            attacker.semiInvulnerabilityData.executionTurn = false;
            _moveDelay = false;
            return;
        }

        attacker.semiInvulnerabilityData.displayMessage = " is underground";
        attacker.semiInvulnerabilityData.onHitMessage = " dug back up!";
        attacker.semiInvulnerabilityData.turnData = new Turn(_currentTurn);
        
        attacker.semiInvulnerabilityData.semiInvulnerabilities.Add(
            new SemiInvulnerability(NameDB.GetMoveName(NameDB.LearnSetMove.Magnitude),2f));
        attacker.semiInvulnerabilityData.semiInvulnerabilities.Add(
            new SemiInvulnerability(NameDB.GetMoveName(NameDB.LearnSetMove.Earthquake),2f));

        attacker.isSemiInvulnerable = true;
        _currentTurn.move.isSureHit = false;
        attacker.semiInvulnerabilityData.executionTurn = true;
        Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" dug underground!");
        _moveDelay = false;
    }

    void fly()
    {
        if (attacker.semiInvulnerabilityData.executionTurn)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName
                                                        + attacker.semiInvulnerabilityData.onHitMessage);
            DisplayDamage(victim);
            attacker.semiInvulnerabilityData.executionTurn = false;
            _moveDelay = false;
            return;
        }

        attacker.semiInvulnerabilityData.displayMessage = " is in the air";
        attacker.semiInvulnerabilityData.onHitMessage = " flew down";
        attacker.semiInvulnerabilityData.turnData = new Turn(_currentTurn);
        
        attacker.semiInvulnerabilityData.semiInvulnerabilities.Add(
            new SemiInvulnerability(NameDB.GetMoveName(NameDB.LearnSetMove.Gust),2f));
        attacker.semiInvulnerabilityData.semiInvulnerabilities.Add(
            new SemiInvulnerability(NameDB.GetMoveName(NameDB.LearnSetMove.Thunder)));
        attacker.semiInvulnerabilityData.semiInvulnerabilities.Add(
            new SemiInvulnerability(NameDB.GetMoveName(NameDB.LearnSetMove.SkyUppercut)));
            
        attacker.isSemiInvulnerable = true;
        _currentTurn.move.isSureHit = false;
        attacker.semiInvulnerabilityData.executionTurn = true;
        Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" flew up high!");
        _moveDelay = false;
    }

    void sandstorm()
    {
        var sandstorm = new WeatherCondition(WeatherCondition.Weather.Sandstorm);
        Turn_Based_Combat.Instance.ChangeWeather(sandstorm);
        _moveDelay = false;
    }

    void raindance()
    {
        var rainDance = new WeatherCondition(WeatherCondition.Weather.Rain);
        Turn_Based_Combat.Instance.ChangeWeather(rainDance);
        _moveDelay = false;
    }
    void morningsun()
    {
        StartCoroutine(HealFromWeather());
    }
    void moonlight()
    {
        StartCoroutine(HealFromWeather());
    }
    private IEnumerator HealFromWeather()
    {
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
        int healthGain = Mathf.FloorToInt(attacker.pokemon.maxHp * fraction);
        
        if (healthGain < 1 && attacker.pokemon.hp < attacker.pokemon.maxHp) healthGain = 1;
        
        Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" restored it's health!");

        HealthGainDisplay(healthGain,healthGainer:attacker);
        yield return new WaitUntil(() => !displayingHealthGain);
        _moveDelay = false;
    }

    void whirlwind()
    {
        StartCoroutine(HandleWhirlwind());
    }

    private IEnumerator HandleWhirlwind()
    {
        if (attacker.pokemon.currentLevel<victim.pokemon.currentLevel)
        {
            Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
            _moveDelay = false;
            yield break;
        }
        if (!Battle_handler.Instance.isTrainerBattle)
        {
            _moveDelay = false;
            Wild_pkm.Instance.inBattle = false;
            Battle_handler.Instance.EndBattle(false,true);
            doingMove = false;
            yield break;
        }
        if (victim.isPlayer)
        {
            var living = Pokemon_party.Instance.GetLivingPokemon();
            if (living.Count < 2)
            {
                Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
                _moveDelay = false;
                yield break;
            }
            
            //exclude current participants
            var excludedIndexes = 1;

            if (Battle_handler.Instance.isDoubleBattle)
                excludedIndexes++;
            
            var randomIndexOfLiving = Utility
                .RandomRange(excludedIndexes, living.Count);
            
            var pokemonAtIndex = Array.IndexOf(Pokemon_party.Instance.party,living[randomIndexOfLiving]);
            var switchData = new SwitchOutData(_currentTurn.victimIndex,pokemonAtIndex,victim);
            
            yield return Turn_Based_Combat.Instance.HandleSwap(switchData,true);
        }
        else
        {
            var enemyTrainer = victim.pokemonTrainerAI;
            var living = enemyTrainer.GetLivingPokemon();
            if (living.Count < 2)
            {
                Dialogue_handler.Instance.DisplayBattleInfo("but it failed!");
                _moveDelay = false;
                yield break;
            }
            
            //exclude current participants
            var excludedIndexes = 1;

            if (Battle_handler.Instance.isDoubleBattle)
                excludedIndexes++;
            
            var randomIndexOfLiving = Utility
                .RandomRange(excludedIndexes, living.Count);
            
            var pokemonAtIndex = enemyTrainer.trainerParty.IndexOf(living[randomIndexOfLiving]);
            
            var switchData = new SwitchOutData(_currentTurn.victimIndex,pokemonAtIndex,victim);
            yield return Turn_Based_Combat.Instance.HandleSwap(switchData,true);
        }
        _moveDelay = false;
    }
    void rest()
    {
        StartCoroutine(HandleRest());
    }

    IEnumerator HandleRest()
    {
        var healthGain = attacker.pokemon.maxHp - attacker.pokemon.hp;
        Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" fell asleep!");
        yield return new WaitForSeconds(1f);
        HealthGainDisplay(healthGain,healthGainer:attacker);
        yield return new WaitUntil(() => !displayingHealthGain);
        attacker.statusHandler.RemoveStatusEffect(true);
        yield return new WaitUntil(()=>attacker.pokemon.statusEffect == PokemonOperations.StatusEffect.None);
        ApplyStatusToVictim(attacker, PokemonOperations.StatusEffect.Sleep, 2);
        yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
        _moveDelay = false;
    }
}