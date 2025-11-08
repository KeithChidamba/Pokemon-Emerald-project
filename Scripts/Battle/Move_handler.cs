using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class Move_handler:MonoBehaviour
{
    public bool doingMove = false;
    public static Move_handler Instance;
    private Turn _currentTurn;
    public Battle_Participant attacker;
    public Battle_Participant victim;
    private readonly float[] _statLevels = {0.25f,0.29f,0.33f,0.4f,0.5f,0.67f,1f,1.5f,2f,2.5f,3f,3.5f,4f};
    private readonly float[] _accuracyAndEvasionLevels = {0.33f,0.375f,0.43f,0.5f,0.6f,0.75f,1f,1.33f,1.67f,2f,2.33f,2.67f,3f};
    private readonly float[] _critLevels = {6.25f,12.5f,25f,50f};
    private Battle_event[] _dialougeOrder={null,null,null,null,null,null,null};
    [SerializeField]private List<OnFieldDamageModifier> _onFieldDamageModifiers = new();
    [SerializeField]private List<DamageDisplayData> _damageDisplayQueue = new();
    [SerializeField]private List<DamageDisplayData> _healhGainQueue = new();
    public bool repeatingMoveCycle;
    private bool _cancelMove;
    public bool processingOrder;
    public bool displayingDamage;
    public bool displayingHealthGain;
    public event Func<Battle_Participant,Battle_Participant,Move,float,float> OnDamageCalc;
    public event Action<float,Battle_Participant> OnDamageDeal;
    public event Action<Battle_Participant,Move> OnMoveHit;
    public event Action<Battle_Participant,PokemonOperations.StatusEffect> OnStatusEffectHit;
    public event Action OnMoveComplete;
    public enum DamageSource{Normal,Burn,Poison,Special}
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
        if (moveEffectiveness == 0 && !_currentTurn.move.isMultiTarget && !_currentTurn.move.hasTypelessEffect && !_currentTurn.move.isSelfTargeted)
            Dialogue_handler.Instance.DisplayBattleInfo("It doesn't affect "+victim.pokemon.pokemonName);
        else
        {
            SetMoveSequence();
            if (_currentTurn.move.effectType != Move.EffectType.PipeLine)
            {
                yield return MoveLogicHandler.Instance.DetermineMoveLogic(attacker,victim,_currentTurn);
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
        
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        yield return new WaitUntil(()=> !displayingDamage);
        yield return new WaitUntil(()=> !displayingHealthGain);
        ResetMoveUsage();
    }

    private void CancelMoveSequence()
    {
        _cancelMove = true;
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
    public float CalculateMoveDamage(Move move,Battle_Participant currentVictim,bool isTypeless=false)
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
                .semiInvulnerabilities.FirstOrDefault(s => s.GetName() == move.moveName);
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
            if (modifier.modifierInfo.typeAffected.ToString() == moveType.typeName)
                return currentDamage * modifier.modifierInfo.damageModifier;
        }
        return currentDamage;
    }
    private float AccountForVictimsBarriers(Move move,Battle_Participant currentVictim,float damage)
    {
        foreach (var barrier in currentVictim.barriers)
        {
            if ((move.isSpecial && barrier.barrierName == NameDB.GetMoveName(NameDB.LearnSetMove.LightScreen))
                || (!move.isSpecial && barrier.barrierName == NameDB.GetMoveName(NameDB.LearnSetMove.Reflect)))
                return  damage-(damage*barrier.barrierEffect);
        }
        return damage;
    }
    public void DisplayEffectiveness(float typeEffectiveness,Battle_Participant currentVictim)
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
        bool isSpecificDamage = false, float predefinedDamage = 0,DamageSource damageSource=DamageSource.Normal) 
    {
        DamageDisplayData data = new DamageDisplayData(currentVictim,displayEffectiveness, isSpecificDamage, predefinedDamage);
        _damageDisplayQueue.Add(data);
        if (!displayingDamage) StartCoroutine(ProcessDamageDisplay(damageSource));
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
    IEnumerator ProcessDamageDisplay(DamageSource damageSource)
    {
       
        displayingDamage = true; 
        while (_damageDisplayQueue.Count > 0)
        {
            var data = _damageDisplayQueue[0];
            StartCoroutine(BattleVisuals.Instance.DisplayDamageTakenVisual(data.affectedParticipant,damageSource));
            yield return new WaitForSeconds(0.5f);
            
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
        DisplayDamage(victim);
        processingOrder = false;
    } 
    private void ResetMoveUsage()
    {
        OnMoveComplete?.Invoke();
        if (repeatingMoveCycle)
        {
            repeatingMoveCycle = false;
            return;
        }
        doingMove = false;
        _cancelMove = false;
    }

    private void CheckVictimVulnerabilityToStatus()
    {
        if (victim.pokemon.statusEffect != PokemonOperations.StatusEffect.None)
        {
            if (_currentTurn.move.moveDamage == 0)
            {//only display message for status-condition-only moves
                var statusRejectionMessage = _currentTurn.move.statusEffect == victim.pokemon.statusEffect?
                    victim.pokemon.pokemonName+" already has a "+victim.pokemon.statusEffect+" effect!"
                    :"but it failed!";
                Dialogue_handler.Instance.DisplayBattleInfo(statusRejectionMessage);
            }
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
        if (Utility.RandomRange(1, 101) > _currentTurn.move.buffOrDebuffChance)
        { processingOrder = false; return;}
        //allows the display of buff message, must be here in case silent buff happened
        BattleOperations.CanDisplayChange = true;
        BattleVisuals.Instance.OnStatVisualDisplayed += NotifyBuffVisualCompletion;
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
    }
    private void NotifyBuffVisualCompletion()
    {
        BattleVisuals.Instance.OnStatVisualDisplayed-=NotifyBuffVisualCompletion;
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
    public void AddFieldDamageModifier(OnFieldDamageModifier newModifier)
    {
        _onFieldDamageModifiers.Add(newModifier);
    }
    public void RemoveFieldDamageModifier(PokemonOperations.Types modifierTypeAffected)
    {
        _onFieldDamageModifiers.RemoveAll(m=>m.modifierInfo.typeAffected==modifierTypeAffected);
    }
    public bool DamageModifierPresent(PokemonOperations.Types type)
    {
        return _onFieldDamageModifiers.Any(m => m.modifierInfo.typeAffected == type);
    }
}