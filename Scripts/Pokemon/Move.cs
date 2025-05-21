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
    public Type type;
    public bool isSpecial;
    [FormerlySerializedAs("is_Buff_Debuff")] public bool isBuffOrDebuff;
    [FormerlySerializedAs("Has_status")] public bool hasStatus;
    [FormerlySerializedAs("Has_effect")] public bool hasSpecialEffect;
    [FormerlySerializedAs("Can_flinch")] public bool canCauseFlinch;
    [FormerlySerializedAs("is_Consecutive")] public bool isConsecutive;
    public bool isMultiTarget;
    public bool isSelfTargeted;
    [FormerlySerializedAs("Priority")] public int priority;
    [FormerlySerializedAs("Powerpoints")] public int powerpoints;
    [FormerlySerializedAs("BasePowerpoints")] public int basePowerpoints;
    [FormerlySerializedAs("max_Powerpoints")] public int maxPowerpoints;
    [FormerlySerializedAs("Status_effect")] public string statusEffect = "None";
    [FormerlySerializedAs("Buff_Debuff")] public string buffOrDebuffName = "None";
    [FormerlySerializedAs("Status_chance")] public float statusChance;
    [FormerlySerializedAs("Debuff_chance")] public float buffOrDebuffChance;
    [FormerlySerializedAs("Description")] public string description;
}