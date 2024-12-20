using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public static class Obj_Instance
{
    public static Move set_move(Move m)
    {
        Move mv = ScriptableObject.CreateInstance<Move>();
        mv.Status_effect = m.Status_effect;
        mv.Move_name = m.Move_name;
        mv.Move_damage = m.Move_damage;
        mv.Move_accuracy = m.Move_accuracy;
        mv.type = m.type;
        mv.isSpecial = m.isSpecial;
        mv.is_Buff_Debuff = m.is_Buff_Debuff;
        mv.Priority = m.Priority;
        mv.Powerpoints = m.Powerpoints;
        mv.max_Powerpoints = m.max_Powerpoints;
        mv.Buff_Debuff = m.Buff_Debuff;
        mv.Status_chance = m.Status_chance;
        mv.Debuff_chance = m.Debuff_chance;
        mv.Description = m.Description;
        mv.player_animtion = m.player_animtion;
        mv.enemy_animtion = m.enemy_animtion;
        return mv;
    }
    public static Pokemon set_Pokemon(Pokemon pkm)
    {
        Pokemon new_pkm = ScriptableObject.CreateInstance<Pokemon>();
        new_pkm.Pokemon_name = pkm.Pokemon_name;
        new_pkm.Pokemon_ID = pkm.Pokemon_ID;
        new_pkm.Personality_value = pkm.Personality_value;
        new_pkm.nature = pkm.nature;
        new_pkm.GenderRatio = pkm.GenderRatio;
        new_pkm.IV = pkm.IV;
        new_pkm.EV = pkm.EV;
        new_pkm.BaseAttack = pkm.BaseAttack;
        new_pkm.BaseDefense = pkm.BaseDefense;
        new_pkm.BaseSP_ATK = pkm.BaseSP_ATK;
        new_pkm.BaseSP_DEF = pkm.BaseSP_DEF;
        new_pkm.Basespeed = pkm.Basespeed;
        new_pkm.Gender = pkm.Gender;
        new_pkm.abilities = pkm.abilities;
        new_pkm.has_gender=pkm.has_gender;
        new_pkm.Base_Pokemon_name = pkm.Base_Pokemon_name;
        new_pkm.Attack = pkm.Attack;
        new_pkm.Accuracy = pkm.Accuracy;
        new_pkm.ability = pkm.ability;
        new_pkm.SP_ATK = pkm.SP_ATK;
        new_pkm.Defense = pkm.Defense;
        new_pkm.SP_DEF = pkm.SP_DEF;
        new_pkm.speed = pkm.speed;
        new_pkm.EVs = pkm.EVs;
        new_pkm.HP_IV =  pkm.HP_IV;
        new_pkm.Attack_IV = pkm.Attack_IV;
        new_pkm.Defense_IV =  pkm.Defense_IV;
        new_pkm.SP_ATK_IV =  pkm.SP_ATK_IV;
        new_pkm.SP_DEF_IV = pkm.SP_DEF_IV;
        new_pkm.speed_IV =  pkm.speed_IV;
        new_pkm.HP_EV =  pkm.HP_EV;
        new_pkm.Attack_EV = pkm.Attack_EV;
        new_pkm.Defense_EV =  pkm.Defense_EV;
        new_pkm.SP_ATK_EV =  pkm.SP_ATK_EV;
        new_pkm.SP_DEF_EV = pkm.SP_DEF_EV;
        new_pkm.speed_EV =  pkm.speed_EV;
        new_pkm.Evasion = pkm.Evasion;
        new_pkm.Buff_Debuffs = pkm.Buff_Debuffs;
        new_pkm.type_Immunity = pkm.type_Immunity;
        new_pkm.CanBeDamaged=pkm.CanBeDamaged;
        new_pkm.Status_effect = pkm.Status_effect;
        new_pkm.learnSet = pkm.learnSet;
        new_pkm.Current_level = pkm.Current_level;
        new_pkm.level_progress = pkm.level_progress;
        new_pkm.exp_yield = pkm.exp_yield;
        new_pkm.has_trainer=pkm.has_trainer;
        new_pkm.canAttack = pkm.canAttack;
        new_pkm.isFlinched=pkm.isFlinched;
        new_pkm.HeldItem = pkm.HeldItem;
        new_pkm.front_picture = pkm.front_picture;
        new_pkm.back_picture = pkm.back_picture;
        new_pkm.HP = pkm.HP;
        new_pkm.max_HP = pkm.max_HP;
        new_pkm.BaseHP = pkm.BaseHP;
        new_pkm.types = pkm.types;
        new_pkm.evolutions = pkm.evolutions;
        new_pkm.evo_line = pkm.evo_line;
        foreach (Move m in pkm.move_set)
            new_pkm.move_set.Add(set_move(m));
        return new_pkm;
    }

    public static Item set_Item(Item item)
    {
        Item new_item = ScriptableObject.CreateInstance<Item>();
        new_item.Item_name = item.Item_name;
        string end_digits = Utility.Get_rand(0,50).ToString() + Utility.Get_rand(0,50).ToString() + Utility.Get_rand(0,50).ToString() + Utility.Get_rand(0,50).ToString();
        new_item.Item_ID = new_item.Item_name + end_digits;
        new_item.Item_type = item.Item_type;
        new_item.Item_desc = item.Item_desc;
        new_item.price = item.price;
        new_item.Item_img = item.Item_img;
        new_item.quantity = item.quantity;
        new_item.Item_effect = item.Item_effect;
        return new_item;
    }
}
