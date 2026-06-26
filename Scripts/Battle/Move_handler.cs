using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class Move_handler:MonoBehaviour,IInjectable
{
    public bool doingMove;
    private Turn _currentTurn;
    public Battle_Participant attacker;
    public Battle_Participant victim;
    private readonly float[] _statLevels = {0.25f,0.29f,0.33f,0.4f,0.5f,0.67f,1f,1.5f,2f,2.5f,3f,3.5f,4f};
    private readonly float[] _accuracyAndEvasionLevels = {0.33f,0.375f,0.43f,0.5f,0.6f,0.75f,1f,1.33f,1.67f,2f,2.33f,2.67f,3f};
    private readonly float[] _critLevels = {6.25f,12.5f,25f,50f};
    [SerializeField]private List<OnFieldDamageModifier> _onFieldDamageModifiers = new();
    [SerializeField]private List<DamageDisplayData> _damageDisplayQueue = new();
    [SerializeField]private List<DamageDisplayData> _healhGainQueue = new();
    public bool repeatingMoveCycle;
    private bool _cancelMove;
    private bool _processingOrder;
    public bool displayingDamage;
    public bool displayingHealthGain;
    public event Func<Battle_Participant,Battle_Participant,Move,float,float> OnDamageCalc;
    public event Action<float,Battle_Participant> OnDamageDeal;
    public event Action<Battle_Participant,Move> OnMoveHit;
    public event Action<Battle_Participant,StatusEffect> OnStatusEffectHit;
    public event Action OnMoveComplete;

    private Dialogue_handler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private BattleVisuals _battleVisualsHandler;
    private Battle_handler _battleHandler;
    private MoveLogicHandler _moveLogicHandler;
    private BattleOperations _battleOperations;
    
    public void Inject(ServiceContainer container)
    {
        _battleOperations = container.Resolve<BattleOperations>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _battleHandler = container.Resolve<Battle_handler>();
        _moveLogicHandler = container.Resolve<MoveLogicHandler>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _battleHandler.OnBattleEnd += ()=> _onFieldDamageModifiers.Clear();
    }
    public void BeginMoveExecution(Turn turn)
    {
        OnMoveComplete = null;
        _currentTurn = turn;
        attacker = _battleHandler.battleParticipants[turn.attackerIndex];
        victim = _battleHandler.battleParticipants[turn.victimIndex];
        StartCoroutine(MoveSequence());
    }
    private IEnumerator MoveSequence()
    {
        var moveEffectiveness = _battleOperations.CheckTypeEffectiveness(victim, _currentTurn.move.type);
        if (moveEffectiveness == 0 && !_currentTurn.move.isMultiTarget && !_currentTurn.move.hasTypelessEffect && !_currentTurn.move.isSelfTargeted)
            _dialogueHandler.DisplayBattleInfo("It doesn't affect "+victim.pokemon.pokemonDisplayName);
        else
        {
            if (_currentTurn.move.effectType != EffectType.PipeLine)
            {
                yield return _moveLogicHandler.DetermineMoveLogic(attacker,victim,_currentTurn);
            }
            else
            {
                var battleSequenceEvents = new List<BattleSequenceEvent>
                {
                    new (DealDamage, _currentTurn.move.moveDamage > 0),
                    new (CheckVictimVulnerabilityToStatus, _currentTurn.move.hasStatus),
                    new (CheckBuffOrDebuffApplicability, _currentTurn.move.isBuffOrDebuff),
                    new (FlinchEnemy, _currentTurn.move.canCauseFlinch),
                    new (ConfuseEnemy, _currentTurn.move.canCauseConfusion),
                    new (TrapEnemy, _currentTurn.move.canTrap),
                    new (InfatuateEnemy, _currentTurn.move.canInfatuate)
                };
                victim.OnPokemonFainted += CancelMoveSequence;//victim faints after damage so the rest of move effect is ignored
                foreach (var battleEvent in battleSequenceEvents)
                {
                    if (_cancelMove)
                        break;
                    yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
                    
                    if (!battleEvent.Condition) continue;
                    _processingOrder = true;
                    battleEvent.Execute();
                    yield return new WaitUntil(() => !_processingOrder);
                    yield return new WaitUntil(()=> !displayingDamage);
                    yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
                } 
                victim.OnPokemonFainted -= CancelMoveSequence;
            }
        }
        
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
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


    private float CalculateConfusionDamage(Battle_Participant confusionVictim)
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
    public IEnumerator DealStruggleDamage(Battle_Participant struggleVictim,Battle_Participant struggleUser,Move struggleMove)
    {
        var struggleDamage = CalculateStruggleDamage(struggleVictim,struggleUser,struggleMove);
        
        DisplayDamage(struggleVictim,displayEffectiveness:false,isSpecificDamage:true
            ,predefinedDamage:struggleDamage);
        
        yield return new WaitUntil(()=> !displayingDamage);
        
        float recoil = Mathf.Floor(struggleUser.pokemon.maxHp * 0.25f);
        
        DisplayDamage(struggleUser,displayEffectiveness:false,isSpecificDamage:true
            ,predefinedDamage:recoil);
        
        yield return new WaitUntil(()=> !displayingDamage);
    }
    private float CalculateStruggleDamage(Battle_Participant currentVictim,Battle_Participant struggleUser,Move struggle)
    {
        var critValue = 1;
        var buffedCritRateIndex = Array.IndexOf(_critLevels, struggleUser.pokemon.critChance);
        float critChance = _critLevels[buffedCritRateIndex];

        if (UnityEngine.Random.Range(0f, 100f) < critChance)
            critValue = 2;

        if (critValue > 1)
            _dialogueHandler.DisplayBattleInfo("Critical Hit!");
        
        float levelFactor = ((struggleUser.pokemon.currentLevel * 2f) / 5f) + 2f;
        
        float attackDefenseRatio = SetAtkDefRatio(critValue, false, struggleUser, currentVictim);
        
        float power = 50f;
        
        float randomFactor = Utility.RandomRange(217, 256) / 255f;
        
        float baseDamage = ((levelFactor * power * attackDefenseRatio) / 50f) + 2f;
        
        float damageModifier = critValue * randomFactor;

        int damageDealt = Mathf.FloorToInt(baseDamage * damageModifier);

        if (damageDealt < 1) damageDealt = 1;
        
        float damageAfterAbilityBuff = OnDamageCalc?.Invoke(struggleUser, victim, struggle, damageDealt) ?? damageDealt;
        float damageAfterFieldModifiers = ApplyFieldDamageModifiers(damageAfterAbilityBuff, struggle.type.typeEnum);
        float finalDamage = AccountForVictimsBarriers(struggle, currentVictim, damageAfterFieldModifiers);

        OnDamageDeal?.Invoke(finalDamage, currentVictim);
        return finalDamage;
    }
    private bool IsInvincible(Move move,Battle_Participant currentVictim)
    {
         if (currentVictim.canBeDamaged || move.moveDamage == 0) return false;
         _dialogueHandler.DisplayBattleInfo(currentVictim.pokemon.pokemonDisplayName+" protected itself");
         
         if (!_currentTurn.move.isMultiTarget) CancelMoveSequence();
         return true;
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
        
        if (critValue > 1f) _dialogueHandler.DisplayBattleInfo("Critical Hit!");
        
        float levelFactor = ((attacker.pokemon.currentLevel * 2f) / 5f) + 2f;
        
        float attackDefenseRatio = SetAtkDefRatio(critValue, move.isSpecial, attacker, currentVictim);

        float stab = _battleOperations.IsStab(attacker.pokemon, move.type) ? 1.5f : 1f;
        
        float typeEffectiveness = isTypeless? 1f 
            :_battleOperations.CheckTypeEffectiveness(currentVictim, move.type);
        
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
        float damageAfterFieldModifiers = ApplyFieldDamageModifiers(damageAfterAbilityBuff,move.type.typeEnum);
        float finalDamage = AccountForVictimsBarriers(move,currentVictim,damageAfterFieldModifiers);
        OnDamageDeal?.Invoke(finalDamage,currentVictim);
        OnMoveHit?.Invoke(attacker,move);
        return finalDamage;
    }
    private float ApplyFieldDamageModifiers(float currentDamage, PokemonType moveType)
    {
        foreach (var modifier in _onFieldDamageModifiers)
        {
            if (modifier.modifierInfo.typeAffected == moveType)
                return currentDamage * modifier.modifierInfo.damageModifier;
        }
        return currentDamage;
    }
    private float AccountForVictimsBarriers(Move move,Battle_Participant currentVictim,float damage)
    {
        foreach (var barrier in currentVictim.barriers)
        {
            if ((move.isSpecial && barrier.barrierName == NameDB.GetMoveName(LearnSetMoveName.LightScreen))
                || (!move.isSpecial && barrier.barrierName == NameDB.GetMoveName(LearnSetMoveName.Reflect)))
                return  damage-(damage*barrier.barrierEffect);
        }
        return damage;
    }
    public void DisplayEffectiveness(float typeEffectiveness,Battle_Participant currentVictim)
    {
        if ((int)math.trunc(typeEffectiveness) == 1) return;
        var message = "";
        if (typeEffectiveness == 0)
            message= "It doesn't affect "+currentVictim.pokemon.pokemonDisplayName+"!";
        else
            message=(typeEffectiveness > 1)?"It's Super effective!":"It's not very effective!";
        _dialogueHandler.DisplayBattleInfo(message);
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
                    ,data.affectedPokemon.healthPhase  * 10f *Time.unscaledDeltaTime);
                displayHp = newHp;
                data.affectedPokemon.hp =  Mathf.Floor(displayHp);
                data.affectedPokemon.ChangeHealth(null);
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
            var damage = data.isSpecificDamage? data.predefinedHealthChange 
                : CalculateMoveDamage(_currentTurn.move, data.affectedParticipant);
            
            if (damage == 0)
            {//protected enemy
                {
                    _damageDisplayQueue.RemoveAt(0);
                    continue;
                }
            }
            
            StartCoroutine(_battleVisualsHandler.DisplayDamageTakenVisual(data.affectedParticipant,damageSource));
            yield return new WaitForSecondsRealtime(0.5f);
            
            var healthAfterChange = Mathf
                .Clamp(data.affectedPokemon.hp - damage,0,data.affectedPokemon.maxHp);
            float displayHp = data.affectedPokemon.hp;
            while (displayHp > healthAfterChange)
            {
                float newHp = Mathf.MoveTowards(displayHp, healthAfterChange,
                    (20f/data.affectedPokemon.healthPhase) * Time.unscaledDeltaTime);
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
                    var typeEffectiveness = _battleOperations
                        .CheckTypeEffectiveness(data.affectedParticipant, _currentTurn.move.type);
                    DisplayEffectiveness(typeEffectiveness,data.affectedParticipant);
                }
            }
            data.affectedPokemon.ChangeHealth(attacker);  
            _damageDisplayQueue.RemoveAt(0);
            yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
            yield return null;
        }
        displayingDamage = false;
    }
    private void DealDamage()
    {
        DisplayDamage(victim);
        _processingOrder = false;
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
        if (victim.pokemon.statusEffect != StatusEffect.None)
        {
            if (_currentTurn.move.moveDamage == 0)
            {//only display message for status-condition-only moves
                var statusRejectionMessage = _currentTurn.move.statusEffect == victim.pokemon.statusEffect?
                    victim.pokemon.pokemonDisplayName+" already has a "+victim.pokemon.statusEffect+" effect!"
                    :"but it failed!";
                _dialogueHandler.DisplayBattleInfo(statusRejectionMessage);
            }
            _processingOrder = false;
            return;
        }
        if (victim.pokemon.hp <= 0){_processingOrder = false; return;}
        if (!victim.canBeDamaged)
        {
            _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName+" protected itself");
            _processingOrder = false;return;
        }
        if (Utility.RandomRange(1, 101) < _currentTurn.move.statusChance)
            if(_currentTurn.move.isMultiTarget)
                foreach (Battle_Participant enemy in attacker.currentEnemies)
                    HandleStatusApplication(enemy,_currentTurn.move,true);
            else{
                HandleStatusApplication(victim,_currentTurn.move,true);
            }
        _processingOrder = false;
    }
    bool CheckInvalidStatusEffect(StatusEffect status,PokemonType typeName,Move move)
    {
        List<(StatusEffect status, PokemonType type)> invalidCombinations = new()
        {
            new(StatusEffect.Poison, PokemonType.Poison),
            new(StatusEffect.BadlyPoison, PokemonType.Poison),
            new(StatusEffect.Burn, PokemonType.Fire),
            new(StatusEffect.Paralysis, PokemonType.Electric),
            new(StatusEffect.Freeze, PokemonType.Ice)
        };
        
        foreach(var invalidCombo in invalidCombinations)
        {
            if (typeName == invalidCombo.type && status == invalidCombo.status)
            {
                if (move.moveDamage == 0) 
                {//if its only a status causing move
                    _dialogueHandler.DisplayBattleInfo("It failed");
                }
                return true;
            }
        }
        return false;
    }
    public void HandleStatusApplication(Battle_Participant currentVictim,Move move, bool displayMessage)
    {
        foreach (var type in currentVictim.pokemon.types)
            if(CheckInvalidStatusEffect(move.statusEffect, type.typeEnum,move))return;
        OnStatusEffectHit?.Invoke(currentVictim,move.statusEffect);
        if (displayMessage)
            _dialogueHandler.DisplayBattleInfo($"{currentVictim.pokemon.pokemonDisplayName} {GetStatusMessage(move.statusEffect)}");
        ApplyStatusToVictim(currentVictim,move.statusEffect);
    }
    private static string GetStatusMessage(StatusEffect status)
    {
         var displayMessage = status switch
            {
                StatusEffect.Paralysis=>"was paralyzed",
                StatusEffect.Burn=>"was burned",
                StatusEffect.BadlyPoison=>"was badly poisoned",
                StatusEffect.Poison=>"was poisoned",
                StatusEffect.Sleep=>"fell fast asleep",
                StatusEffect.Freeze=>"was frozen",
                _=>""
            };
        return displayMessage;
    }
    public void ApplyStatusToVictim(Battle_Participant participant,StatusEffect status, int numTurns=0)
    {
        participant.pokemon.statusEffect = status;
        var numTurnsOfStatus = 0;
        if (numTurns != 0)
            participant.statusHandler.GetStatusEffect(numTurns);
        else
        {
            if(status==StatusEffect.Sleep)
                numTurnsOfStatus = Utility.RandomRange(1, 5);
        }
        participant.statusHandler.GetStatusEffect(numTurnsOfStatus);
    }

    public void ApplyStatChangeImmunity(Battle_Participant participant,StatChangeability changeability,int numTurns)
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
            _dialogueHandler.DisplayBattleInfo(enemy.pokemon.pokemonDisplayName+ "can't be trapped");
            _processingOrder = false;
            return;
        }
       
        if(isMoveEffect)
        {
            if (enemy.pokemon.HasType(PokemonType.Ghost)
                && !attacker.pokemon.HasType(PokemonType.Ghost))
            {//only ghost can trap ghost with moves
                _dialogueHandler.DisplayBattleInfo(enemy.pokemon.pokemonDisplayName+ "can't be trapped");
                _processingOrder = false;
                return;
            }

            if (!enemy.canEscape)
            {
                _processingOrder = false;
                _dialogueHandler.DisplayBattleInfo(enemy.pokemon.pokemonDisplayName + " is already trapped");
                return;
            }
           
            if (_currentTurn.move.statusChance == 0)
                enemy.statusHandler.SetupTrapDuration(hasDuration:false);
            else
            {
                var numTurnsOfTrap = Utility.RandomRange(2, (int)_currentTurn.move.statusChance+1);
                enemy.statusHandler.SetupTrapDuration(numTurnsOfTrap,_currentTurn.move);
            }
            _processingOrder = false;
            return;
        }
        enemy.statusHandler.SetupTrapDuration(hasDuration:false);
    }
    void ConfuseEnemy()
    {
        if (victim.isConfused)
        {
            _processingOrder = false;
            return;
        }
        if (_currentTurn.move.isSelfTargeted)
            ApplyConfusion(attacker);
        else
        {
            if (victim.canBeDamaged)
                ApplyConfusion(victim);
            else
                _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName + " protected itself");
        }
        _processingOrder = false;
    }

    void ApplyConfusion(Battle_Participant victimOfConfusion)
    {
        if (Utility.RandomRange(1, 101) <= _currentTurn.move.statusChance)
        {
            var randomNumTurns = Utility.RandomRange(1, 6);
            _dialogueHandler.DisplayBattleInfo(victimOfConfusion.pokemon.pokemonDisplayName
                                                        + " was confused");
            victimOfConfusion.statusHandler.GetConfusion(randomNumTurns);
        }
    }
    void FlinchEnemy()
    {
        if (!victim.canBeDamaged || !victim.pokemon.canBeFlinched)
        {
            _processingOrder = false;
            return;
        }
        if (Utility.RandomRange(1, 101) < _currentTurn.move.statusChance)
        {
            victim.canAttack = false;
            victim.isFlinched = true;
        }
        _processingOrder = false;
    }

    void InfatuateEnemy()
    {
        if (victim.isInfatuated)
        {
            _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName+" is already in love!");
            _processingOrder = false;
            return;
        }
        if (!victim.canBeDamaged || !victim.pokemon.canBeInfatuated)
        {
            _processingOrder = false;
            return;
        }
        if (victim.pokemon.gender == Gender.None 
            || attacker.pokemon.gender == Gender.None
            || attacker.pokemon.gender == victim.pokemon.gender)
        {
            _dialogueHandler.DisplayBattleInfo("but it failed!");
            _processingOrder = false;
            return;
        }
        victim.isInfatuated = true;
    }


    void CheckBuffOrDebuffApplicability()
    {
        if (Utility.RandomRange(1, 101) > _currentTurn.move.buffOrDebuffChance)
        {
            _processingOrder = false; 
            return;
        }
        StartCoroutine(HandleBuffOrDebuffApplication());
    }
    private IEnumerator HandleBuffOrDebuffApplication()
    {
        //allows the display of buff message, must be here in case silent buff happened before
        _battleOperations.canDisplayChange = true;
        
        foreach (var buffData in _currentTurn.move.buffOrDebuffData)
        {
            if (!_currentTurn.move.isSelfTargeted)
            {//affecting enemy
                if ( (_currentTurn.move.isMultiTarget && !_battleHandler.isDoubleBattle) 
                     || !_currentTurn.move.isMultiTarget)
                {
                    if (!victim.canBeDamaged || victim.ProtectedFromStatChange(buffData.isIncreasing))
                    {
                        _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName + " protected itself");
                    }
                    else
                    {
                        var data = new BuffDebuffData(victim, buffData.stat, buffData.isIncreasing, buffData.amount);
                        yield return ExecuteSequentialBuffOrDebuff(data);
                    }
                } 
                if(_currentTurn.move.isMultiTarget && _battleHandler.isDoubleBattle)
                {
                    yield return MultiTargetBuff_Debuff(buffData.stat, buffData.isIncreasing, buffData.amount);
                }
            }
            else//affecting attacker
            {
                var data = new BuffDebuffData(attacker, buffData.stat, buffData.isIncreasing, buffData.amount);
                yield return ExecuteSequentialBuffOrDebuff(data);
            }
        }
        _processingOrder = false;
    }
    private IEnumerator ExecuteSequentialBuffOrDebuff(BuffDebuffData data)
    {
        bool awaitingCompletion = true;
        _battleVisualsHandler.OnStatVisualDisplayed += NotifyBuffVisualCompletion;
        ExecuteBuffOrDebuff(data);
        yield return new WaitUntil(() => !awaitingCompletion);
        void NotifyBuffVisualCompletion()
        {
            _battleVisualsHandler.OnStatVisualDisplayed-=NotifyBuffVisualCompletion;
            awaitingCompletion = false;
        }
    }
    private IEnumerator MultiTargetBuff_Debuff(Stat stat, bool isIncreasing,int buffAmount)
    {
        foreach (Battle_Participant enemy in new List<Battle_Participant>(attacker.currentEnemies) )
        {
            if (enemy.canBeDamaged && !victim.ProtectedFromStatChange(isIncreasing))
            {
                var data = new BuffDebuffData(enemy, stat, isIncreasing,buffAmount);
                yield return ExecuteSequentialBuffOrDebuff(data);
            }
            else
                _dialogueHandler.DisplayBattleInfo(enemy.pokemon.pokemonDisplayName + " protected itself");
            yield return new WaitUntil(()=>!_dialogueHandler.messagesLoading);
        }
    }
    
    public void ExecuteBuffOrDebuff(BuffDebuffData data)
    {
        var unModifiedStats = data.Receiver.statData;
        var affectedPokemon = data.Receiver.pokemon;

        switch (data.Stat)
        {
            case Stat.Defense:
                affectedPokemon.defense = GetUpdatedStat(unModifiedStats.defense,data) ?? affectedPokemon.defense;
                break;
            case Stat.Attack:
                affectedPokemon.attack = GetUpdatedStat(unModifiedStats.attack,data) ?? affectedPokemon.attack;
                break;
            case Stat.SpecialDefense:
                affectedPokemon.specialDefense = GetUpdatedStat(unModifiedStats.spDef,data) ?? affectedPokemon.specialDefense;
                break;
            case Stat.SpecialAttack:
                affectedPokemon.specialAttack = GetUpdatedStat(unModifiedStats.spAtk,data) ?? affectedPokemon.specialAttack;
                break;
            case Stat.Speed:
                affectedPokemon.speed = GetUpdatedStat(unModifiedStats.speed,data) ?? affectedPokemon.speed;
                break;
            case Stat.Accuracy:
                affectedPokemon.accuracy = GetUpdatedStat(unModifiedStats.accuracy,data) ?? affectedPokemon.accuracy;
                break;
            case Stat.Evasion:
                affectedPokemon.evasion = GetUpdatedStat(unModifiedStats.evasion,data) ?? affectedPokemon.evasion;
                break;
            case Stat.Crit:
                affectedPokemon.critChance = GetUpdatedStat(unModifiedStats.crit,data)?? affectedPokemon.critChance;
                break; 
        }
    }
    private float? GetUpdatedStat(float unmodifiedStatValue, BuffDebuffData data)
    {
        _battleOperations.ChangeOrCreateBuffOrDebuff(data);
        var buff = _battleOperations.SearchForBuffOrDebuff(data.Receiver.pokemon, data.Stat)
                   ?? new Buff_Debuff(Stat.None, 0, true); // if null return same value
        if (buff.isAtLimit) return null;
        return ModifyStatValue(data.Stat, unmodifiedStatValue, buff.stage);
    }

    public float ModifyStatValue(Stat stat, float unmodifiedStatValue ,int buffStage)
    {
        switch (stat)
        {
            case Stat.Accuracy:
            case Stat.Evasion:
                return math.trunc(unmodifiedStatValue * _accuracyAndEvasionLevels[buffStage+6]);
            case Stat.Crit:
                return _critLevels[buffStage];
            default:
                return math.trunc(unmodifiedStatValue * _statLevels[buffStage+6]); 
        }
    }
    public bool HasDuplicateBarrier(Battle_Participant currentParticipant,string  barrierName,bool displayMessage)
    {
        var duplicateBarrier = currentParticipant.barriers.Any(b => b.barrierName == barrierName); 

        if (_battleHandler.isDoubleBattle)
        {
            var partner= _battleHandler
                .battleParticipants[currentParticipant.GetPartnerIndex()];
                
            if(partner.isActive)
                if(partner.barriers.Any(b => b.barrierName == barrierName))
                {
                    duplicateBarrier = true;
                }
        }

        if (duplicateBarrier && displayMessage)
            _dialogueHandler.DisplayBattleInfo(barrierName + " is already activated");
        
        return duplicateBarrier;
    }
    public void AddFieldDamageModifier(OnFieldDamageModifier newModifier)
    {
        _onFieldDamageModifiers.Add(newModifier);
    }
    public void RemoveFieldDamageModifier(PokemonType modifierTypeAffected)
    {
        _onFieldDamageModifiers.RemoveAll(m=>m.modifierInfo.typeAffected==modifierTypeAffected);
    }
    public bool DamageModifierPresent(PokemonType type)
    {
        return _onFieldDamageModifiers.Any(m => m.modifierInfo.typeAffected == type);
    }
}

public enum DamageSource{Normal,Burn,Poison,Special}