using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Move", menuName = "p_Move")]
public class Move : ScriptableObject
{
    public string Move_name;
    public float Move_damage;
    public float Move_accuracy;
    public Type type;
    public bool isSpecial = false;
    public bool is_Buff_Debuff = false;
    public bool Has_status = false;
    public bool Has_effect = false;
    public bool Can_flinch = false;
    public int Priority=0;
    public int Powerpoints;
    public int max_Powerpoints;
    public string Status_effect="None";
    public string Buff_Debuff="None";//e-DEF
    public float Status_chance = 0;
    public float Debuff_chance = 0;
    public string Description;
    public Animation player_animtion;
    public Animation enemy_animtion;
}
