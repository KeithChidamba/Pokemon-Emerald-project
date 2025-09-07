using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Move", menuName = "p_Move")]
public class Move : ScriptableObject
{
    [FormerlySerializedAs("Move_name")] public string moveName;
    [FormerlySerializedAs("Move_damage")] public float moveDamage;
    [FormerlySerializedAs("Move_accuracy")] public float moveAccuracy;
    public int critModifierIndex;
    public Type type;
    public bool hasTypelessEffect;
    public bool isSpecial;
    public bool isContact;
    [FormerlySerializedAs("is_Buff_Debuff")] public bool isBuffOrDebuff;
    [FormerlySerializedAs("Has_status")] public bool hasStatus;
    [FormerlySerializedAs("Has_effect")] public bool hasSpecialEffect;
    [FormerlySerializedAs("Can_flinch")] public bool canCauseFlinch;
    public bool canTrap;
    public bool canCauseConfusion;
    public bool canInfatuate;
    [FormerlySerializedAs("is_Consecutive")] public bool isConsecutive;
    public bool isMultiTarget;
    public bool isSelfTargeted;
    public bool isSureHit;
    public bool displayTargetMessage;
    [FormerlySerializedAs("Priority")] public int priority;
    [FormerlySerializedAs("Powerpoints")] public int powerpoints;
    [FormerlySerializedAs("BasePowerpoints")] public int basePowerpoints;
    [FormerlySerializedAs("max_Powerpoints")] public int maxPowerpoints;
    public PokemonOperations.StatusEffect statusEffect;
    public List<MoveBuffData> buffOrDebuffData = new();
    [FormerlySerializedAs("Status_chance")] public float statusChance;
    [FormerlySerializedAs("Debuff_chance")] public float buffOrDebuffChance;
    [FormerlySerializedAs("Description")] public string description;
}