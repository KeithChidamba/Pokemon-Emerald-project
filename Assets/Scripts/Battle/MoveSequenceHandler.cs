using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public enum DamageCalculationModifier
{
    Barrier,Ability,FieldModifiers,SemiInvulnerable
}
public class MoveSequenceHandler:MonoBehaviour,IInjectable
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
    
    public event Func<BattleParticipant,BattleParticipant,Move,float,float> OnDamageCalc;
    public event Action<DamageCalculationModifier,float, float> OnDamageModified;
    public event Action<float,BattleParticipant> OnDamageDeal;
    public event Action<BattleParticipant,BattleParticipant,Move> OnMoveHit;
    public event Action<BattleParticipant,StatusEffect> OnStatusEffectHit;
    public event Action OnMoveComplete;
    private List<Func<BattleParticipant, float, Stat, float>> _statModifiers = new();
    
    private DialogueHandler _dialogueHandler;
    private BattleVisuals _battleVisualsHandler;
    private BattleHandler _battleHandler;
    private MoveLogicHandler _moveLogicHandler;
    private BattleOperations _battleOperations;
    
    public void Inject(ServiceContainer container)
    {
        _battleOperations = container.Resolve<BattleOperations>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _battleHandler = container.Resolve<BattleHandler>();
        _moveLogicHandler = container.Resolve<MoveLogicHandler>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _battleHandler.OnBattleEnd += ClearState;
    }

    private void ClearState()
    {
        _onFieldDamageModifiers.Clear();
    }
    public void SubToMoveStatUpdate(Func<BattleParticipant, float, Stat, float> subscriber)
    {
        Debug.Log($"stat update : Added - {subscriber.Method.Name}/{subscriber.GetHashCode()}");
        if (!_statModifiers.Contains(subscriber))
        {
            _statModifiers.Add(subscriber);
        }
        else
        {
            Debug.LogError("Duplicate Subscriber for stat update");
        } 
        Debug.Log($"stat update subscribers: {_statModifiers.Count}");
    }
    public void UnsubscribeFromStatUpdate(Func<BattleParticipant, float, Stat, float> subscriber)
    {
        Debug.Log($"stat update : Removed - {subscriber.Method.Name}/{subscriber.GetHashCode()}");
        _statModifiers.Remove(subscriber);
        Debug.Log($"Stat Update subscribers: {_statModifiers.Count}");
    }
    
    public void BeginMoveExecution(Turn turn)
    {
        doingMove = true;
        OnMoveComplete = null;
        var attacker = _battleHandler.GetParticipant(turn.attackerKey);
        var victim = _battleHandler.GetParticipant(turn.victimKey);
        StartCoroutine(MoveSequence(turn,attacker,victim));
    }
    private IEnumerator MoveSequence(Turn currentTurn,BattleParticipant attacker,BattleParticipant victim)
    {
        var move = currentTurn.move;
        var moveEffectiveness = _battleOperations.CheckTypeEffectiveness(victim, move.type);
        if (moveEffectiveness == 0 && !move.isMultiTarget
                                   && !move.hasTypelessEffect 
                                   && !move.isSelfTargeted)
        {
            _dialogueHandler.DisplayBattleInfo("It doesn't affect " + victim.pokemon.pokemonDisplayName);
        }
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
                    new (CheckStatChangeApplicability, move.canChangeStats),
                    new (FlinchEnemy, move.canCauseFlinch),
                    new (ConfuseEnemy, move.canCauseConfusion),
                    new (TrapEnemy, move.canTrap),
                    new (InfatuateEnemy, move.canInfatuate)
                };
                _battleHandler.OnFaintSequenceComplete += CancelMoveSequence;
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
                _battleHandler.OnFaintSequenceComplete -= CancelMoveSequence;
            }
        }
        
        yield return _dialogueHandler.AwaitAllDialogue();
        yield return AwaitDamageDisplay();
        yield return AwaitHealthGainDisplay();
        ResetMoveUsage();
        void CancelMoveSequence(BattleParticipant faintedParticipant)
        {
            if(faintedParticipant == victim)
            {
                //victim faints after damage, so the rest of move effect is ignored
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
    public IEnumerator DealConfusionDamage(BattleParticipant confusionVictim)
    {
        var confusionDamage = CalculateConfusionDamage(confusionVictim);
        
        DisplaySpecialDamage(confusionVictim,predefinedDamage:confusionDamage);
        
        yield return AwaitDamageDisplay();
    }
    
    private float CalculateConfusionDamage(BattleParticipant confusionVictim)
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
    public IEnumerator DealStruggleDamage(BattleParticipant struggleVictim,BattleParticipant struggleUser,Move struggleMove)
    {
        var struggleDamage = CalculateStruggleDamage(struggleVictim,struggleUser,struggleMove);
        
        DisplaySpecialDamage(struggleVictim, predefinedDamage:struggleDamage);
        
        yield return AwaitDamageDisplay();
        
        float recoil = Mathf.Floor(struggleUser.pokemon.maxHp * 0.25f);
        
        DisplaySpecialDamage(struggleUser,predefinedDamage:recoil);
        
        yield return AwaitDamageDisplay();
    }
    private float CalculateStruggleDamage(BattleParticipant victim,BattleParticipant struggleUser,Move struggle)
    {
        var critValue = GetCritValue(struggleUser.pokemon);
        if (critValue > 1)
        {
            _dialogueHandler.DisplayBattleInfo("Critical Hit!");
        }
        
        float levelFactor = ((struggleUser.pokemon.currentLevel * 2f) / 5f) + 2f;
        
        float attackDefenseRatio = SetAtkDefRatio(critValue, false, struggleUser, victim);
        
        float power = 50f;
        
        float randomFactor = Utility.RandomRange(217, 256) / 255f;
        
        float baseDamage = ((levelFactor * power * attackDefenseRatio) / 50f) + 2f;
        
        float damageModifier = critValue * randomFactor;

        int damageDealt = Mathf.FloorToInt(baseDamage * damageModifier);
        
        float damageAfterAbilityBuff = OnDamageCalc?.Invoke(struggleUser, victim, struggle, damageDealt) ?? damageDealt;
        damageAfterAbilityBuff = Mathf.FloorToInt(damageAfterAbilityBuff);
        if (damageAfterAbilityBuff > damageDealt)
        {
            OnDamageModified?.Invoke(DamageCalculationModifier.Ability,damageDealt,damageAfterAbilityBuff);
        }
        
        int finalDamage = Mathf.FloorToInt(AccountForVictimsBarriers(struggle, victim, damageAfterAbilityBuff));
        if(finalDamage > damageAfterAbilityBuff || finalDamage < damageAfterAbilityBuff)
        {
            OnDamageModified?.Invoke(DamageCalculationModifier.Barrier, damageAfterAbilityBuff, finalDamage);
        }
        
        OnDamageDeal?.Invoke(finalDamage, victim);
        OnMoveHit?.Invoke(struggleUser,victim,struggle);
        return finalDamage;
    }

    private int GetCritValue(Pokemon pokemon)
    {
        if (!_critLevels.Contains(pokemon.critChance))
        {
            return 1;
        }
        if (Utility.RandomRange100() < pokemon.critChance)
        {
            return 2;
        }
        return 1;
    }
    private bool IsInvincible(Move move,BattleParticipant victim)
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
    public float CalculateMoveDamage(Move move,BattleParticipant attacker,BattleParticipant victim,bool isTypeless=false)
    {
        if (move.moveDamage == 0) return 0;
        
        if (IsInvincible(move, victim)) return 0;
        
        //calc crit
        var critValue = GetCritValue(attacker.pokemon);
        
        if (critValue > 1f) _dialogueHandler.DisplayBattleInfo("Critical Hit!");
        
        float levelFactor = ((attacker.pokemon.currentLevel * 2f) / 5f) + 2f;
        
        float attackDefenseRatio = SetAtkDefRatio(critValue, move.isSpecial, attacker, victim);

        float stab = _battleOperations.IsStab(attacker.pokemon, move.type) ? 1.5f : 1f;
        
        float typeEffectiveness = isTypeless? 1f 
            :_battleOperations.CheckTypeEffectiveness(victim, move.type);
        
        float randomFactor = Utility.RandomRange(217, 256) / 255f;

        float baseDamage = ((levelFactor * move.moveDamage * attackDefenseRatio) / 50f) + 2f;
        
        var semiInvulnerableMod = GetSemiInvulnerableModifiers(victim, move.moveName);
        if (semiInvulnerableMod == 0)
        {
            Debug.LogError($"Semi invulnerable data has an invalid damage multiplier of 0 move name: {move.moveName}");
        }
        
        var damageAfterSemiBuff = baseDamage * semiInvulnerableMod;
        if (semiInvulnerableMod > 1f || semiInvulnerableMod < 1f)
        {
            OnDamageModified?.Invoke(DamageCalculationModifier.SemiInvulnerable,baseDamage,damageAfterSemiBuff);
        }
        baseDamage = damageAfterSemiBuff;
        
        float damageModifier = critValue * stab * typeEffectiveness * randomFactor;
        
        int damageDealt = Mathf.FloorToInt(baseDamage * damageModifier);
        
        var damageAfterAbilityBuff = OnDamageCalc?.Invoke(attacker,victim,move,damageDealt) ?? damageDealt;
        damageAfterAbilityBuff = Mathf.FloorToInt(damageAfterAbilityBuff);
        if (damageAfterAbilityBuff > damageDealt)
        {
            OnDamageModified?.Invoke(DamageCalculationModifier.Ability,damageDealt,damageAfterAbilityBuff);
        }
        
        int damageAfterFieldModifiers = Mathf.FloorToInt(ApplyFieldDamageModifiers(damageAfterAbilityBuff,move.type.typeEnum));
        
        //Users of this specific event will check if the damage changed themselves
        OnDamageModified?.Invoke(DamageCalculationModifier.FieldModifiers,damageAfterAbilityBuff,damageAfterFieldModifiers);
        
        
        int finalDamage = Mathf.FloorToInt(AccountForVictimsBarriers(move,victim,damageAfterFieldModifiers));
        if(finalDamage > damageAfterFieldModifiers || finalDamage < damageAfterFieldModifiers)
        {
            OnDamageModified?.Invoke(DamageCalculationModifier.Barrier, damageAfterFieldModifiers, finalDamage);
        }
        
        OnDamageDeal?.Invoke(finalDamage,victim);
        OnMoveHit?.Invoke(attacker,victim,move);
        return finalDamage;
    }

    private float GetSemiInvulnerableModifiers(BattleParticipant victim,string moveName)
    {
        if(victim.isSemiInvulnerable)
        { 
            var semiInvulnerability = victim.semiInvulnerabilityData
                .semiInvulnerabilities.FirstOrDefault(s => s.GetName() == moveName);
            if (semiInvulnerability != null)
            {
                return semiInvulnerability.damageMultiplier;
            }
        }
        return 1f;
    }
    private float ApplyFieldDamageModifiers(float currentDamage, PokemonType moveType)
    {
        float modifier = 1f;

        foreach (var fieldEffect in _onFieldDamageModifiers)
        {
            foreach (var damageModifier in fieldEffect.modifierInfo.damageModifiers)
            {
                if (damageModifier.typeAffected == moveType)
                {
                    modifier *= damageModifier.damageFactor;
                }
            }
        }
        return currentDamage * modifier;
    }
    private float AccountForVictimsBarriers(Move move,BattleParticipant victim,float damage)
    {
        foreach (var barrier in victim.barriers)
        {
            if ((move.isSpecial && barrier.barrierName == NameDB.GetMoveName(MoveName.LightScreen))
                || (!move.isSpecial && barrier.barrierName == NameDB.GetMoveName(MoveName.Reflect)))
                return  damage-(damage*barrier.barrierEffect);
        }
        return damage;
    }
    public void DisplayEffectiveness(float typeEffectiveness,BattleParticipant victim)
    {
        if ((int)math.trunc(typeEffectiveness) == 1) return;
        var message = "";
        if (typeEffectiveness == 0)
            message= "It doesn't affect "+victim.pokemon.pokemonDisplayName+"!";
        else
            message=(typeEffectiveness > 1)?"It's Super effective!":"It's not very effective!";
        _dialogueHandler.DisplayBattleInfo(message);
    }
    private float SetAtkDefRatio(int crit, bool isSpecial, BattleParticipant currentAttacker, BattleParticipant victim)
    {
        float atk, def;
        bool canIgnoreStages = currentAttacker.pokemon.currentLevel >= victim.pokemon.currentLevel  && crit == 2;
        if (!isSpecial)
        {
            atk = canIgnoreStages && currentAttacker.statData.attack < currentAttacker.pokemon.attack
                ? currentAttacker.pokemon.attack  // Ignore stat change
                : currentAttacker.statData.attack;
            
            def = canIgnoreStages && victim.statData.defense > victim.pokemon.defense
                ? victim.pokemon.defense  // Ignore stat change
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
    public void HealthGainDisplay(float healthGained,Pokemon affectedPokemon = null,BattleParticipant healthGainer = null)
    {
        var data = new DamageDisplayData(DamageSource.Normal,
            affectedParticipant:healthGainer
            ,healthChange:healthGained,
            affectedPokemon:affectedPokemon);
        
        _healhGainQueue.Add(data);
        if (!displayingHealthGain) StartCoroutine(ProcessHealthGainDisplay());
    }
    
    public void DisplaySpecialDamage(BattleParticipant victim, float predefinedDamage
        ,DamageSource damageSource=DamageSource.Normal) 
    {
        var data = new DamageDisplayData(damageSource,affectedParticipant:victim,
            displayEffectiveness:false
            , healthChange:predefinedDamage);
        
        _damageDisplayQueue.Add(data);
        if (!displayingDamage) StartCoroutine(ProcessDamageDisplay());
    }
    
    public void DisplayMoveDamage(Move move,BattleParticipant attacker, BattleParticipant victim
        , bool displayEffectiveness = true)
    {
        var damage = CalculateMoveDamage(move,attacker, victim);
        DisplaySpecificMoveDamage(move,victim,damage,displayEffectiveness);
    }
    
    public void DisplaySpecificMoveDamage(Move move,BattleParticipant victim,
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
    private void DealDamage(Move move,BattleParticipant attacker, BattleParticipant victim)
    {
        DisplayMoveDamage(move,attacker,victim);
        _processingOrder = false;
    } 
    
    private void CheckVictimVulnerabilityToStatus(Move move,BattleParticipant attacker, BattleParticipant victim)
    {
        if (victim.pokemon.statusEffect != StatusEffect.None)
        {
            if (move.moveDamage == 0)
            {
                //only display message for status-condition-only moves
                var statusRejectionMessage = move.statusEffect == victim.pokemon.statusEffect?
                    $"{victim.pokemon.pokemonDisplayName} already has a {victim.pokemon.statusEffect} effect!"
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
            _processingOrder = false;
            return;
        }
        if (Utility.RandomRange100() <= move.statusChance)
        {
            if (move.isMultiTarget)
            {
                foreach (BattleParticipant enemy in attacker.currentEnemies)
                {
                    HandleStatusApplication(enemy, move, true);
                }
            }
            else
            {
                HandleStatusApplication(victim, move, true);
            }
        }
        _processingOrder = false;
    }
    private bool CheckInvalidStatusEffect(StatusEffect status,PokemonType typeName,Move move)
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
    public void HandleStatusApplication(BattleParticipant victim,Move move, bool displayMessage)
    {
        foreach (var type in victim.pokemon.types)
        {
            if (CheckInvalidStatusEffect(move.statusEffect, type.typeEnum, move))
            {
                return;
            }
        }
        OnStatusEffectHit?.Invoke(victim,move.statusEffect);
        if (displayMessage)
        {
            _dialogueHandler.DisplayBattleInfo($"{victim.pokemon.pokemonDisplayName} {GetStatusMessage(move.statusEffect)}");
        }
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
    public void ApplyStatusToVictim(BattleParticipant participant,StatusEffect status, int numTurns=0)
    {
        var numTurnsOfStatus = 0;
        if (numTurns != 0)
        {
            participant.statusHandler.GetStatusEffect(status,numTurns);
        }
        else
        {
            if(status==StatusEffect.Sleep)
            {
                numTurnsOfStatus = Utility.GetRandomChance(CommonRandom.Rnd5);
            }
        }
        participant.statusHandler.GetStatusEffect(status,numTurnsOfStatus);
    }

    public void ApplyStatChangeImmunity(BattleParticipant participant,StatChangeability changeability,int numTurns)
    {
        if (!participant.isActive) return;
        participant.statusHandler.GetStatChangeImmunity(changeability,numTurns);
    }

    private void TrapEnemy(Move move,BattleParticipant attacker, BattleParticipant victim)
    {
        var trapData = move.GetDynamicModule<TrapDataInfo>();

        if (trapData.trapType == TrapDataInfo.TrapType.RandomDurationFromMove)
        {
            trapData.SetRandomDuration();
        }
        
        _battleHandler.OnSwitchOut += RemoveOnSwitchOrFaint;
        victim.statusHandler.SetupTrapDuration(trapData);
        _processingOrder = false;
        return;
        void RemoveOnSwitchOrFaint(BattleParticipant switcher)
        {
            if (switcher.participantKey == attacker.participantKey)
            {
                _battleHandler.OnSwitchOut -= RemoveOnSwitchOrFaint;
                victim.statusHandler.RemoveTrap(trapData.trapType);
            }
        }
    }
    
    /// <summary>
    /// For use in general trapping logic, outside move sequence.
    /// Does not have dedicated trap messages.
    /// </summary>
    public void ApplyTrap(BattleParticipant victim,TrapDataInfo.TrapType type, int numTurns=0)
    {
        var trapData = new TrapDataInfo { trapType = type ,trapDuration = numTurns};
        victim.statusHandler.SetupTrapDuration(trapData,false);
    }
    void ConfuseEnemy(Move move,BattleParticipant attacker, BattleParticipant victim)
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

    void ApplyConfusion(BattleParticipant victimOfConfusion,Move move)
    {
        if (Utility.RandomRange100() <= move.statusChance)
        {
            var randomNumTurns = Utility.GetRandomChance(CommonRandom.Rnd5);
            _dialogueHandler.DisplayBattleInfo(victimOfConfusion.pokemon.pokemonDisplayName
                                                        + " was confused");
            victimOfConfusion.statusHandler.GetConfusion(randomNumTurns);
        }
    }
    void FlinchEnemy(Move move,BattleParticipant attacker, BattleParticipant victim)
    {
        if (!victim.canBeDamaged || !victim.canBeFlinched)
        {
            _processingOrder = false;
            return;
        }
        if (Utility.RandomRange100() <= move.statusChance)
        {
            victim.canAttack = false;
            victim.isFlinched = true;
        }
        _processingOrder = false;
    }

    void InfatuateEnemy(Move move,BattleParticipant attacker, BattleParticipant victim)
    {
        if (victim.isInfatuated)
        {
            _dialogueHandler.DisplayBattleInfo(victim.pokemon.pokemonDisplayName+" is already in love!");
            _processingOrder = false;
            return;
        }
        if (!victim.canBeDamaged || !victim.canBeInfatuated)
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

        _battleHandler.OnSwitchOut += RemoveOnSwitchOrFaint;
        victim.isInfatuated = true;
        _processingOrder = false;
        return;
        void RemoveOnSwitchOrFaint(BattleParticipant switcher)
        {
            if (switcher.participantKey == attacker.participantKey)
            {
                _battleHandler.OnSwitchOut -= RemoveOnSwitchOrFaint;
                victim.isInfatuated = false;
            }
        }
    }


    void CheckStatChangeApplicability(Move move,BattleParticipant attacker, BattleParticipant victim)
    {
        if (Utility.RandomRange100() <= move.buffOrDebuffChance)
        {
            StartCoroutine(HandleStatChangeApplication(move, attacker, victim));
        }
        else
        {
            _processingOrder = false; 
        }
    }
    private IEnumerator HandleStatChangeApplication(Move move,BattleParticipant attacker, BattleParticipant victim)
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
                        var data = new StatChangeTransitData(victim, buffData.stat, buffData.isIncreasing, buffData.amount);
                        yield return ExecuteSequentialStatChange(data);
                    }
                } 
                if(move.isMultiTarget && _battleHandler.isDoubleBattle)
                {
                    yield return MultiTargetStatChange(attacker,victim,buffData.stat, buffData.isIncreasing, buffData.amount);
                }
            }
            else//affecting attacker
            {
                var data = new StatChangeTransitData(attacker, buffData.stat, buffData.isIncreasing, buffData.amount);
                yield return ExecuteSequentialStatChange(data);
            }
        }
        _processingOrder = false;
    }
    private IEnumerator ExecuteSequentialStatChange(StatChangeTransitData data)
    {
        bool awaitingCompletion = true;
        _battleVisualsHandler.OnStatVisualDisplayed += NotifyStatVisualCompletion;
        InitiateStatChange(data);
        yield return new WaitUntil(() => !awaitingCompletion);
        void NotifyStatVisualCompletion()
        {
            _battleVisualsHandler.OnStatVisualDisplayed-=NotifyStatVisualCompletion;
            awaitingCompletion = false;
        }
    }
    private IEnumerator MultiTargetStatChange(BattleParticipant attacker, BattleParticipant victim
        ,Stat stat, bool isIncreasing,int changeAmount)
    {
        foreach (var enemy in new List<BattleParticipant>(attacker.currentEnemies) )
        {
            if (enemy.canBeDamaged && !victim.ProtectedFromStatChange(isIncreasing))
            {
                var data = new StatChangeTransitData(enemy, stat, isIncreasing,changeAmount);
                yield return ExecuteSequentialStatChange(data);
            }
            else
                _dialogueHandler.DisplayBattleInfo(enemy.pokemon.pokemonDisplayName + " protected itself");
            yield return _dialogueHandler.AwaitAllDialogue();
        }
    }
    
    public void InitiateStatChange(StatChangeTransitData data,bool displayMessage = true)
    {
        var unModifiedStats = data.receiver.statData;
        var affectedPokemon = data.receiver.pokemon;

        switch (data.stat)
        {
            case Stat.Defense:
                affectedPokemon.defense = GetUpdatedStat(unModifiedStats.defense,data, displayMessage);
                break;
            case Stat.Attack:
                affectedPokemon.attack = GetUpdatedStat(unModifiedStats.attack,data, displayMessage);
                break;
            case Stat.SpecialDefense:
                affectedPokemon.specialDefense = GetUpdatedStat(unModifiedStats.spDef,data, displayMessage);
                break;
            case Stat.SpecialAttack:
                affectedPokemon.specialAttack = GetUpdatedStat(unModifiedStats.spAtk,data, displayMessage);
                break;
            case Stat.Speed:
                affectedPokemon.speed = GetUpdatedStat(unModifiedStats.speed,data, displayMessage);
                break;
            case Stat.Accuracy:
                affectedPokemon.accuracy = GetUpdatedStat(unModifiedStats.accuracy,data, displayMessage);
                break;
            case Stat.Evasion:
                affectedPokemon.evasion = GetUpdatedStat(unModifiedStats.evasion,data, displayMessage);
                break;
            case Stat.Crit:
                affectedPokemon.critChance = GetUpdatedStat(unModifiedStats.crit,data, displayMessage);
                break; 
        }
    }

    public void RefreshStat(Stat stat, BattleParticipant receiver)
    {
        var statChangeData = new StatChangeTransitData(receiver, stat, true, 0);
        switch (stat)
        {
            case Stat.Attack:
                receiver.pokemon.attack = GetUpdatedStat(
                    receiver.statData.attack, statChangeData, false);
                break;

            case Stat.Defense:
                receiver.pokemon.defense = GetUpdatedStat(
                    receiver.statData.defense, statChangeData, false);
                break;

            case Stat.SpecialAttack:
                receiver.pokemon.specialAttack = GetUpdatedStat(
                    receiver.statData.spAtk, statChangeData, false);
                break;

            case Stat.SpecialDefense:
                receiver.pokemon.specialDefense = GetUpdatedStat(
                    receiver.statData.spDef, statChangeData, false);
                break;

            case Stat.Speed:
                receiver.pokemon.speed = GetUpdatedStat(
                    receiver.statData.speed, statChangeData, false);
                break;

            case Stat.Accuracy:
                receiver.pokemon.accuracy = GetUpdatedStat(
                    receiver.statData.accuracy, statChangeData, false);
                break;

            case Stat.Evasion:
                receiver.pokemon.evasion = GetUpdatedStat(
                    receiver.statData.evasion, statChangeData, false);
                break;

            case Stat.Crit:
                receiver.pokemon.critChance = GetUpdatedStat(
                    receiver.statData.crit, statChangeData, false);
                break;
        }
    }

    private float GetUpdatedStat(float unmodifiedStatValue, StatChangeTransitData data,bool canDisplayChange)
    {
        var resultMessage = _battleOperations.AttemptStatChangeOperation(data);
        if (canDisplayChange)
        {
            _battleVisualsHandler.SelectStatChangeVisuals(data.stat,data.receiver, resultMessage);
        }
        var statChange = _battleOperations.SearchForStatModifier(data.receiver.pokemon, data.stat);
        if(statChange.stage == 0)
        {
            //remove because it's neutral, but still return that neutral stat value
            data.receiver.pokemon.statModifiers.RemoveAll(b => b.stat == data.stat);
        }
        var updatedStat= ModifyStatValue(data.stat, unmodifiedStatValue, statChange.stage);
        
        float statAfterExternalModifiers = updatedStat;
        Debug.Log($"Initial stat value: {statAfterExternalModifiers}");
        foreach (var handler in _statModifiers)
        {
            var currentUpdate = handler.Invoke(data.receiver,statAfterExternalModifiers,data.stat);
            if(statAfterExternalModifiers > currentUpdate || statAfterExternalModifiers < currentUpdate)
            {
                Debug.Log($"accounted for {data.stat}");
            }
            statAfterExternalModifiers = currentUpdate;
        }
        Debug.Log($"Final stat value: {statAfterExternalModifiers}");
        return Mathf.FloorToInt(statAfterExternalModifiers);
    }

    private float ModifyStatValue(Stat stat, float unmodifiedStatValue ,int stage)
    {
        switch (stat)
        {
            case Stat.Accuracy:
            case Stat.Evasion:
                return Mathf.FloorToInt(unmodifiedStatValue * _accuracyAndEvasionLevels[stage+6]);
            case Stat.Crit:
                return _critLevels[stage];
            default:
                return Mathf.FloorToInt(unmodifiedStatValue * _statLevels[stage+6]); 
        }
    }
    public bool HasDuplicateBarrier(BattleParticipant currentParticipant,string  barrierName,bool displayMessage)
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

    public void AddFieldDamageModifier(OnFieldDamageModifier newFieldModifier)
    {
        _onFieldDamageModifiers.Add(newFieldModifier);
    }
    public void RemoveFieldDamageModifier(DamageModifierSource source)
    {
        _onFieldDamageModifiers.RemoveAll(m=>m.modifierInfo.modifierSource == source);
    }
    public bool FieldDamageSourceExists(DamageModifierSource source)
    {
       return _onFieldDamageModifiers.Any(m=>m.modifierInfo.modifierSource == source);
    }
}

public enum DamageSource{Normal,Burn,Poison,Special}