using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Move", menuName = "Pokemon/Move")]
public class Move : InfoModuleHolder
{
    [FormerlySerializedAs("Move_name")] public string moveName;
    [FormerlySerializedAs("Description")] public string description;
    public PokemonTypeData type;
    public EffectType effectType;
    
    public bool hasTypelessEffect;
    public bool isSpecial; 
    public bool isContact;
        
    [FormerlySerializedAs("isBuffOrDebuff")] 
    [FormerlySerializedAs("is_Buff_Debuff")] 
    public bool canChangeStats;
    [FormerlySerializedAs("Has_status")] public bool hasStatus;
    [FormerlySerializedAs("Can_flinch")] public bool canCauseFlinch;
    public bool canTrap;
    public bool canCauseConfusion;
    public bool canInfatuate;
    public bool isMultiTarget;
    public bool isSelfTargeted;
    public bool isSureHit;
    public bool displayTargetMessage;
    
    [FormerlySerializedAs("Move_damage")] public float moveDamage;
    [FormerlySerializedAs("Move_accuracy")] public float moveAccuracy;
    public int critModifierIndex;
    [FormerlySerializedAs("Priority")] public int priority;
    [FormerlySerializedAs("Powerpoints")] public int powerpoints;
    [FormerlySerializedAs("BasePowerpoints")] public int basePowerpoints;
    [FormerlySerializedAs("max_Powerpoints")] public int maxPowerpoints;
    
    public StatusEffect statusEffect;
    public List<MoveStatChangeData> buffOrDebuffData = new();
    [FormerlySerializedAs("Status_chance")] public float statusChance;
    [FormerlySerializedAs("Debuff_chance")] public float buffOrDebuffChance;
}

public enum EffectType
{
    PipeLine,UniqueLogic,MultiTargetDamage,Consecutive,HealthDrain,WeatherHealthGain,WeatherChange
    ,IdentifyTarget, BarrierCreation,DamageProtection,OnFieldDamageModifier,SemiInvulnerable,SpecificDamage
};