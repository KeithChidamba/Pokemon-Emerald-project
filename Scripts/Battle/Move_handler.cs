using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class Move_handler:MonoBehaviour,IInjectable
{
    private readonly float[] _statLevels = {0.25f,0.29f,0.33f,0.4f,0.5f,0.67f,1f,1.5f,2f,2.5f,3f,3.5f,4f};
    private readonly float[] _accuracyAndEvasionLevels = {0.33f,0.375f,0.43f,0.5f,0.6f,0.75f,1f,1.33f,1.67f,2f,2.33f,2.67f,3f};
    private readonly float[] _critLevels = {6.25f,12.5f,25f,50f};
    
    [SerializeField]private List<OnFieldDamageModifier> _onFieldDamageModifiers = new();
    [SerializeField]private List<DamageDisplayData> _damageDisplayQueue = new();
    [SerializeField]private List<DamageDisplayData> _healhGainQueue = new();
    
    [SerializeField]private bool doingMove;
    [SerializeField]private bool repeatingMoveCycle;
    private bool _cancelMove;
    private bool _processingOrder;
    [SerializeField]private bool displayingDamage;
    [SerializeField]private bool displayingHealthGain;
    
    public event Func<Battle_Participant,Battle_Participant,Move,float,float> OnDamageCalc;
    public event Action<float,Battle_Participant> OnDamageDeal;
    public event Action<Battle_Participant,Move> OnMoveHit;
    public event Action<Battle_Participant,StatusEffect> OnStatusEffectHit;
    public event Action OnMoveComplete;

    private Dialogue_handler _dialogueHandler;
    private BattleVisuals _battleVisualsHandler;
    private Battle_handler _battleHandler;
    private MoveLogicHandler _moveLogicHandler;
    private BattleOperations _battleOperations;
    
    public void Inject(ServiceContainer container)
    {
        _battleOperations = container.Resolve<BattleOperations>();
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
        doingMove = true;
        OnMoveComplete = null;
        var attacker = _battleHandler.GetParticipant(turn.attackerKey);
        var victim = _battleHandler.GetParticipant(turn.victimKey);
        StartCoroutine(MoveSequence(turn,attacker,victim));
    }
    private IEnumerator MoveSequence(Turn currentTurn,Battle_Participant attacker,Battle_Participant victim)
    {
        var move = currentTurn.move;
        var moveEffectiveness = _battleOperations.CheckTypeEffectiveness(victim, move.type);
        if (moveEffectiveness == 0 && !move.isMultiTarget && !move.hasTypelessEffect && !move.isSelfTargeted)
            _dialogueHandler.DisplayBattleInfo("It doesn't affect "+victim.pokemon.pokemonDisplayName);
        else
        {
            if (move.effectType != EffectType.PipeLine)
            {
                yield return _moveLogicHandler.DetermineMoveLogic(attacker,victim,currentTurn);
            }
            else
            {
                var battleSequenceEvents = new List<BattleSequenceEvent>
                {
                    new (DealDamage, move.moveDamage > 0),
                    new (CheckVictimVulnerabilityToStatus, move.hasStatus),
                    new (CheckBuffOrDebuffApplicability, move.isBuffOrDebuff),
                    new (FlinchEnemy, move.canCauseFlinch),
                    new (ConfuseEnemy, move.canCauseConfusion),
                    new (TrapEnemy, move.canTrap),
                    new (InfatuateEnemy, move.canInfatuate)
                };
                _battleHandler.OnParticipantFainted += CancelMoveSequence;
                foreach (var battleEvent in battleSequenceEvents)
                {
                    if (_cancelMove)
                        break;
                    yield return _dialogueHandler.AwaitAllDialogue();
                    
                    if (!battleEvent.Condition) continue;
                    _processingOrder = true;
                    battleEvent.Execute(move,attacker,victim);
                    yield return new WaitUntil(() => !_processingOrder);
                    yield return AwaitDamageDisplay();
                    yield return _dialogueHandler.AwaitAllDialogue();
                } 
                _battleHandler.OnParticipantFainted -= CancelMoveSequence;
            }
        }
        
        yield return _dialogueHandler.AwaitAllDialogue();
        yield return AwaitDamageDisplay();
        yield return AwaitHealthGainDisplay();
        ResetMoveUsage();
        void CancelMoveSequence(Battle_Participant faintedParticipant)
        {
            if(faintedParticipant == victim)
            {
                //victim faints after damage so the rest of move effect is ignored
                _cancelMove = true;
            }
        }
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
    public void AllowMoveRepeat()
    {
        repeatingMoveCycle = true;
    }
    public void ResetAfterBattleTermination()
    {
        doingMove = false;
    }
    public IEnumerator AwaitMoveCompletion()
    {
        yield return new WaitUntil(() => !doingMove);
    }
    public IEnumerator AwaitDamageDisplay()
    {
        yield return new WaitUntil(()=> !displayingDamage);
    }
    public IEnumerator AwaitHealthGainDisplay()
    {
        yield return new WaitUntil(()=> !displayingHealthGain);
    }
    public IEnumerator DealConfusionDamage(Battle_Participant confusionVictim)
    {
        var confusionDamage = CalculateConfusionDamage(confusionVictim);
        
        DisplaySpecialDamage(confusionVictim,predefinedDamage:confusionDamage);
        
        yield return AwaitDamageDisplay();
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

        return damage;
    }
    public IEnumerator DealStruggleDamage(Battle_Participant struggleVictim,Battle_Participant struggleUser,Move struggleMove)
    {
        var struggleDamage = CalculateStruggleDamage(struggleVictim,struggleUser,struggleMove);
        
        DisplaySpecialDamage(struggleVictim, predefinedDamage:struggleDamage);
        
        yield return AwaitDamageDisplay();
        
        float recoil = Mathf.Floor(struggleUser.pokemon.maxHp * 0.25f);
        
        DisplaySpecialDamage(struggleUser,predefinedDamage:recoil);
        
        yield return AwaitDamageDisplay();
    }
    private float CalculateStruggleDamage(Battle_Participant victim,Battle_Participant struggleUser,Move struggle)
    {
        var critValue = 1;
        var buffedCritRateIndex = Array.IndexOf(_critLevels, struggleUser.pokemon.critChance);
        float critChance = _critLevels[buffedCritRateIndex];

        if (UnityEngine.Random.Range(0f, 100f) < critChance)
            critValue = 2;

        if (critValue > 1)
            _dialogueHandler.DisplayBattleInfo("Critical Hit!");
        
        float levelFactor = ((struggleUser.pokemon.currentLevel * 2f) / 5f) + 2f;
        
        float attackDefenseRatio = SetAtkDefRatio(critValue, false, struggleUser, victim);
        
        float power = 50f;
        
        float randomFactor = Utility.RandomRange(217, 256) / 255f;
        
        float baseDamage = ((levelFactor * power * attackDefenseRatio) / 50f) + 2f;
        
        float damageModifier = critValue * randomFactor;

        int damageDealt = Mathf.FloorToInt(baseDamage * damageModifier);
        
        float damageAfterAbilityBuff = OnDamageCalc?.Invoke(struggleUser, victim, struggle, damageDealt) ?? damageDealt;
        float damageAfterFieldModifiers = ApplyFieldDamageModifiers(damageAfterAbilityBuff, struggle.type.typeEnum);
        float finalDamage = AccountForVictimsBarriers(struggle, victim, damageAfterFieldModifiers);

        OnDamageDeal?.Invoke(finalDamage, victim);
        return finalDamage;
    }
    private bool IsInvincible(Move move,Battle_Participant victim)
    {
         if (victim.canBeDamaged) return false;
         
         _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName+" protected itself");
         
         if (!move.isMultiTarget)
         {
             //cancel early because victim is protected
             _cancelMove = true;
         }
         return true;
    }
    public float CalculateMoveDamage(Move move,Battle_Participant attacker,Battle_Participant victim,bool isTypeless=false)
    {
        if (move.moveDamage == 0) return 0;
        
        if (IsInvincible(move, victim)) return 0;
        
        //calc crit
        var critValue = 1;
        var buffedCritRateIndex = Array.IndexOf(_critLevels, attacker.pokemon.critChance)
                                  + move.critModifierIndex;
        float critChance = _critLevels[buffedCritRateIndex];
        if (UnityEngine.Random.Range(0f, 100f) < critChance)
            critValue =  2;
        
        if (critValue > 1f) _dialogueHandler.DisplayBattleInfo("Critical Hit!");
        
        float levelFactor = ((attacker.pokemon.currentLevel * 2f) / 5f) + 2f;
        
        float attackDefenseRatio = SetAtkDefRatio(critValue, move.isSpecial, attacker, victim);

        float stab = _battleOperations.IsStab(attacker.pokemon, move.type) ? 1.5f : 1f;
        
        float typeEffectiveness = isTypeless? 1f 
            :_battleOperations.CheckTypeEffectiveness(victim, move.type);
        
        float randomFactor = Utility.RandomRange(217, 256) / 255f;

        float baseDamage = ((levelFactor * move.moveDamage * attackDefenseRatio) / 50f) + 2f;
                
        if(victim.isSemiInvulnerable)
        { 
            var semiInvulnerability = victim.semiInvulnerabilityData
                .semiInvulnerabilities.FirstOrDefault(s => s.GetName() == move.moveName);
            baseDamage *= semiInvulnerability?.damageMultiplier ?? 1f;
        }
        
        float damageModifier = critValue * stab * typeEffectiveness * randomFactor;
        
        int damageDealt = Mathf.FloorToInt(baseDamage * damageModifier);
        
        float damageAfterAbilityBuff = OnDamageCalc?.Invoke(attacker,victim,move,damageDealt) ?? damageDealt;
        float damageAfterFieldModifiers = ApplyFieldDamageModifiers(damageAfterAbilityBuff,move.type.typeEnum);
        float finalDamage = AccountForVictimsBarriers(move,victim,damageAfterFieldModifiers);
        OnDamageDeal?.Invoke(finalDamage,victim);
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
    private float AccountForVictimsBarriers(Move move,Battle_Participant victim,float damage)
    {
        foreach (var barrier in victim.barriers)
        {
            if ((move.isSpecial && barrier.barrierName == NameDB.GetMoveName(LearnSetMoveName.LightScreen))
                || (!move.isSpecial && barrier.barrierName == NameDB.GetMoveName(LearnSetMoveName.Reflect)))
                return  damage-(damage*barrier.barrierEffect);
        }
        return damage;
    }
    public void DisplayEffectiveness(float typeEffectiveness,Battle_Participant victim)
    {
        if ((int)math.trunc(typeEffectiveness) == 1) return;
        var message = "";
        if (typeEffectiveness == 0)
            message= "It doesn't affect "+victim.pokemon.pokemonDisplayName+"!";
        else
            message=(typeEffectiveness > 1)?"It's Super effective!":"It's not very effective!";
        _dialogueHandler.DisplayBattleInfo(message);
    }
    private float SetAtkDefRatio(int crit, bool isSpecial, Battle_Participant currentAttacker, Battle_Participant victim)
    {
        float atk, def;
        bool canIgnoreStages = currentAttacker.pokemon.currentLevel >= victim.pokemon.currentLevel  && crit == 2;
        if (!isSpecial)
        {
            atk = canIgnoreStages && currentAttacker.statData.attack < currentAttacker.pokemon.attack
                ? currentAttacker.pokemon.attack  // Ignore debuff
                : currentAttacker.statData.attack;
            
            def = canIgnoreStages && victim.statData.defense > victim.pokemon.defense
                ? victim.pokemon.defense  // Ignore buff
                : victim.statData.defense;
        }
        else
        {
            atk = canIgnoreStages && currentAttacker.statData.spAtk < currentAttacker.pokemon.specialAttack
                ? currentAttacker.pokemon.specialAttack
                : currentAttacker.statData.spAtk;
            
            def = canIgnoreStages && victim.statData.spDef > victim.pokemon.specialDefense
                ? victim.pokemon.specialDefense
                : victim.statData.spDef;
        }
        return atk / def;
    }
    public void HealthGainDisplay(float healthGained,Pokemon affectedPokemon = null,Battle_Participant healthGainer = null)
    {
        var data = new DamageDisplayData(DamageSource.Normal,
            affectedParticipant:healthGainer
            ,healthChange:healthGained,
            affectedPokemon:affectedPokemon);
        
        _healhGainQueue.Add(data);
        if (!displayingHealthGain) StartCoroutine(ProcessHealthGainDisplay());
    }
    
    public void DisplaySpecialDamage(Battle_Participant victim, float predefinedDamage
        ,DamageSource damageSource=DamageSource.Normal) 
    {
        var data = new DamageDisplayData(damageSource,affectedParticipant:victim,
            displayEffectiveness:false
            , healthChange:predefinedDamage);
        
        _damageDisplayQueue.Add(data);
        if (!displayingDamage) StartCoroutine(ProcessDamageDisplay());
    }
    
    public void DisplayMoveDamage(Move move,Battle_Participant attacker, Battle_Participant victim
        , bool displayEffectiveness = true)
    {
        var damage = CalculateMoveDamage(move,attacker, victim);
        DisplaySpecificMoveDamage(move,victim,damage,displayEffectiveness);
    }
    
    public void DisplaySpecificMoveDamage(Move move,Battle_Participant victim,
        float specificDamage,bool displayEffectiveness = true) 
    {
        var typeEffectiveness = _battleOperations.CheckTypeEffectiveness(victim, move.type);
        
        var data = new DamageDisplayData(DamageSource.Normal,
            affectedParticipant:victim
            ,displayEffectiveness:displayEffectiveness,healthChange:specificDamage
            ,effectivenessScore:typeEffectiveness);
        
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
                                                + data.healthChange,0,data.affectedPokemon.maxHp);
            
            float displayHp = data.affectedPokemon.hp;
            while (displayHp < healthAfterChange)
            {
                float newHp = Mathf.MoveTowards(displayHp, healthAfterChange
                    ,data.affectedPokemon.healthPhase  * 10f *Time.unscaledDeltaTime);
                displayHp = newHp;
                data.affectedPokemon.hp =  Mathf.Floor(displayHp);
                data.affectedPokemon.NotifyHealthChange();
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
            var damage = data.healthChange;
            
            if (damage == 0)
            {//protected enemy
                {
                    _damageDisplayQueue.RemoveAt(0);
                    continue;
                }
            }
            StartCoroutine(_battleVisualsHandler.DisplayDamageTakenVisual(data.affectedParticipant,data.damageSource));
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
                DisplayEffectiveness(data.effectivenessScore,data.affectedParticipant);
            }
            data.affectedPokemon.NotifyHealthChange();  
            _damageDisplayQueue.RemoveAt(0);
            yield return _dialogueHandler.AwaitAllDialogue();
            yield return null;
        }
        displayingDamage = false;
    }
    private void DealDamage(Move move,Battle_Participant attacker, Battle_Participant victim)
    {
        DisplayMoveDamage(move,attacker,victim);
        _processingOrder = false;
    } 
    
    private void CheckVictimVulnerabilityToStatus(Move move,Battle_Participant attacker, Battle_Participant victim)
    {
        if (victim.pokemon.statusEffect != StatusEffect.None)
        {
            if (move.moveDamage == 0)
            {//only display message for status-condition-only moves
                var statusRejectionMessage = move.statusEffect == victim.pokemon.statusEffect?
                    victim.pokemon.pokemonDisplayName+" already has a "+victim.pokemon.statusEffect+" effect!"
                    :"but it failed!";
                _dialogueHandler.DisplayBattleInfo(statusRejectionMessage);
            }
            _processingOrder = false;
            return;
        }

        if (victim.pokemon.hp <= 0)
        {
            _processingOrder = false; 
            return;
        }
        
        if (!victim.canBeDamaged)
        {
            _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName+" protected itself");
            _processingOrder = false;return;
        }
        if (Utility.RandomRange(1, 101) < move.statusChance)
            if(move.isMultiTarget)
                foreach (Battle_Participant enemy in attacker.currentEnemies)
                    HandleStatusApplication(enemy,move,true);
            else{
                HandleStatusApplication(victim,move,true);
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
    public void HandleStatusApplication(Battle_Participant victim,Move move, bool displayMessage)
    {
        foreach (var type in victim.pokemon.types)
            if(CheckInvalidStatusEffect(move.statusEffect, type.typeEnum,move))return;
        OnStatusEffectHit?.Invoke(victim,move.statusEffect);
        if (displayMessage)
            _dialogueHandler.DisplayBattleInfo($"{victim.pokemon.pokemonDisplayName} {GetStatusMessage(move.statusEffect)}");
        ApplyStatusToVictim(victim,move.statusEffect);
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

    private bool CanTrapEnemy(Battle_Participant victim)
    {
        if (victim.pokemon.ability.abilityName == AbilityName.Levitate)
        {
            _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName+ "can't be trapped");
            return false;
        }
        return true;
    }
    private void TrapEnemy(Move move,Battle_Participant attacker, Battle_Participant victim)
    {
        if (!CanTrapEnemy(victim))
        {
            _processingOrder = false;
            return;
        }
        
        if (victim.pokemon.HasType(PokemonType.Ghost)
            && !attacker.pokemon.HasType(PokemonType.Ghost))
        {//only ghost can trap ghost with moves
            _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName+ "can't be trapped");
            _processingOrder = false;
            return;
        }

        if (!victim.canEscape)
        {
            _processingOrder = false;
            _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName + " is already trapped");
            return;
        }
       
        if (move.statusChance == 0)
        {
            victim.statusHandler.SetupTrapDuration(hasDuration: false);
        }
        else
        {
            var numTurnsOfTrap = Utility.RandomRange(2, (int)move.statusChance+1);
            victim.statusHandler.SetupTrapDuration(numTurnsOfTrap,move);
        }
        _processingOrder = false;
    }
    public void ApplyTrap(Battle_Participant victim)
    {
        if (!CanTrapEnemy(victim)) return;
        victim.statusHandler.SetupTrapDuration(hasDuration:false);
    }
    void ConfuseEnemy(Move move,Battle_Participant attacker, Battle_Participant victim)
    {
        if (victim.isConfused)
        {
            _processingOrder = false;
            return;
        }
        if (move.isSelfTargeted)
            ApplyConfusion(attacker,move);
        else
        {
            if (victim.canBeDamaged)
                ApplyConfusion(victim,move);
            else
                _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName + " protected itself");
        }
        _processingOrder = false;
    }

    void ApplyConfusion(Battle_Participant victimOfConfusion,Move move)
    {
        if (Utility.RandomRange(1, 101) <= move.statusChance)
        {
            var randomNumTurns = Utility.RandomRange(1, 6);
            _dialogueHandler.DisplayBattleInfo(victimOfConfusion.pokemon.pokemonDisplayName
                                                        + " was confused");
            victimOfConfusion.statusHandler.GetConfusion(randomNumTurns);
        }
    }
    void FlinchEnemy(Move move,Battle_Participant attacker, Battle_Participant victim)
    {
        if (!victim.canBeDamaged || !victim.pokemon.canBeFlinched)
        {
            _processingOrder = false;
            return;
        }
        if (Utility.RandomRange(1, 101) < move.statusChance)
        {
            victim.canAttack = false;
            victim.isFlinched = true;
        }
        _processingOrder = false;
    }

    void InfatuateEnemy(Move move,Battle_Participant attacker, Battle_Participant victim)
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


    void CheckBuffOrDebuffApplicability(Move move,Battle_Participant attacker, Battle_Participant victim)
    {
        if (Utility.RandomRange(1, 101) > move.buffOrDebuffChance)
        {
            _processingOrder = false; 
            return;
        }
        StartCoroutine(HandleBuffOrDebuffApplication(move,attacker, victim));
    }
    private IEnumerator HandleBuffOrDebuffApplication(Move move,Battle_Participant attacker, Battle_Participant victim)
    {
        foreach (var buffData in move.buffOrDebuffData)
        {
            if (!move.isSelfTargeted)
            {//affecting enemy
                if ( (move.isMultiTarget && !_battleHandler.isDoubleBattle) 
                     || !move.isMultiTarget)
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
                if(move.isMultiTarget && _battleHandler.isDoubleBattle)
                {
                    yield return MultiTargetBuff_Debuff(attacker,victim,buffData.stat, buffData.isIncreasing, buffData.amount);
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
    private IEnumerator MultiTargetBuff_Debuff(Battle_Participant attacker, Battle_Participant victim
        ,Stat stat, bool isIncreasing,int buffAmount)
    {
        foreach (var enemy in new List<Battle_Participant>(attacker.currentEnemies) )
        {
            if (enemy.canBeDamaged && !victim.ProtectedFromStatChange(isIncreasing))
            {
                var data = new BuffDebuffData(enemy, stat, isIncreasing,buffAmount);
                yield return ExecuteSequentialBuffOrDebuff(data);
            }
            else
                _dialogueHandler.DisplayBattleInfo(enemy.pokemon.pokemonDisplayName + " protected itself");
            yield return _dialogueHandler.AwaitAllDialogue();
        }
    }
    
    public void ExecuteBuffOrDebuff(BuffDebuffData data,bool displayMessage = true)
    {
        var unModifiedStats = data.Receiver.statData;
        var affectedPokemon = data.Receiver.pokemon;

        switch (data.Stat)
        {
            case Stat.Defense:
                affectedPokemon.defense = GetUpdatedStat(unModifiedStats.defense,data, displayMessage) ?? affectedPokemon.defense;
                break;
            case Stat.Attack:
                affectedPokemon.attack = GetUpdatedStat(unModifiedStats.attack,data, displayMessage) ?? affectedPokemon.attack;
                break;
            case Stat.SpecialDefense:
                affectedPokemon.specialDefense = GetUpdatedStat(unModifiedStats.spDef,data, displayMessage) ?? affectedPokemon.specialDefense;
                break;
            case Stat.SpecialAttack:
                affectedPokemon.specialAttack = GetUpdatedStat(unModifiedStats.spAtk,data, displayMessage) ?? affectedPokemon.specialAttack;
                break;
            case Stat.Speed:
                affectedPokemon.speed = GetUpdatedStat(unModifiedStats.speed,data, displayMessage) ?? affectedPokemon.speed;
                break;
            case Stat.Accuracy:
                affectedPokemon.accuracy = GetUpdatedStat(unModifiedStats.accuracy,data, displayMessage) ?? affectedPokemon.accuracy;
                break;
            case Stat.Evasion:
                affectedPokemon.evasion = GetUpdatedStat(unModifiedStats.evasion,data, displayMessage) ?? affectedPokemon.evasion;
                break;
            case Stat.Crit:
                affectedPokemon.critChance = GetUpdatedStat(unModifiedStats.crit,data, displayMessage) ?? affectedPokemon.critChance;
                break; 
        }
    }
    private float? GetUpdatedStat(float unmodifiedStatValue, BuffDebuffData data,bool canDisplayChange)
    {
        var resultMessage = _battleOperations.AttemptBuffOperation(data);
        if (canDisplayChange)
        {
            _battleVisualsHandler.SelectStatChangeVisuals(data.Stat,data.Receiver, resultMessage);
        }
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
            var partner = currentParticipant.GetPartner();
                
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