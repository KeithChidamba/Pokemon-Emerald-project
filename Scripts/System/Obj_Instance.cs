using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obj_Instance : MonoBehaviour
{
    public Bag bag;
    public Move set_move(Move m)
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
    public Pokemon set_Pokemon(Pokemon pkm)
    {
        Pokemon new_pkm = ScriptableObject.CreateInstance<Pokemon>();
        new_pkm.Pokemon_name = pkm.Pokemon_name;
        new_pkm.Pokemon_ID = pkm.Pokemon_ID;
        new_pkm.Base_Pokemon_name = pkm.Base_Pokemon_name;
        new_pkm.Attack = pkm.Attack;
        new_pkm.Accuracy = pkm.Accuracy;
        new_pkm.ability = pkm.ability;
        new_pkm.SP_ATK = pkm.SP_ATK;
        new_pkm.Defense = pkm.Defense;
        new_pkm.SP_DEF = pkm.SP_DEF;
        new_pkm.speed = pkm.speed;
        new_pkm.type_Immunity = pkm.type_Immunity;
        new_pkm.Status_effect = pkm.Status_effect;
        new_pkm.learnSet = pkm.learnSet;
        new_pkm.Current_level = pkm.Current_level;
        new_pkm.level_progress = pkm.level_progress;
        new_pkm.front_picture = pkm.front_picture;
        new_pkm.back_picture = pkm.back_picture;
        new_pkm.HP = pkm.HP;
        new_pkm.max_HP = pkm.max_HP;
        new_pkm.types = pkm.types;
        new_pkm.evolutions = pkm.evolutions;
        new_pkm.evo_line = pkm.evo_line;
        int i = 0;
        foreach (Move m in pkm.move_set)
        {
            if (m != null)
            {
                new_pkm.move_set[i] = set_move(m);
            }
            i++;
        }
        return new_pkm;
    }
    int Get_rand(int exclusive_lim)
    {
        return UnityEngine.Random.Range(0, exclusive_lim);
    }
    public Item set_Item(Item item)
    {
        Item new_item = ScriptableObject.CreateInstance<Item>();
        new_item.Item_name = item.Item_name;
        string end_digits = Get_rand(bag.max_capacity).ToString() + Get_rand(bag.max_capacity).ToString() + Get_rand(bag.max_capacity).ToString() + Get_rand(bag.max_capacity).ToString();
        new_item.Item_ID = new_item.Item_name + end_digits;
        new_item.Item_type = item.Item_type;
        new_item.Item_desc = item.Item_desc;
        new_item.price = item.price;
        new_item.Item_img = item.Item_img;
        new_item.quantity = item.quantity;
        return new_item;
    }
}
