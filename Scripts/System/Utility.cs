using System.Collections.Generic;
using UnityEngine;

public static class Utility
{
    public static string removeSpace(string name_)
    {
        char splitter = ' ';
        int space_count = 0;
        List<int> num_spaces = new();
        for (int i = 0; i < name_.Length; i++)
        {
            if (name_[i] == splitter)
            {
                num_spaces.Add(i);
                space_count++;
            }
        }
        string result = "";
        if (space_count > 0)
        {
            int index = 0;
            for (int i = 0; i < space_count; i++)
            {
                result += name_.Substring(index,(num_spaces[i]-index));
                index = num_spaces[i]+1;
            }
            //part after last space
            result+=name_.Substring(num_spaces[space_count - 1]+1, (name_.Length - num_spaces[space_count - 1]-1));
        }
        else
        {
            result = name_;
        }
        return result;
    }
    public static int Get_rand(int min,int exclusive_lim)
    {
        return UnityEngine.Random.Range(min, exclusive_lim);
    }
    public static bool isImmuneTo(Pokemon victim,Type enemy_type)
    {
        for (int i = 0; i < victim.num_types; i++)
        {
            if (victim.types[i].type_check(victim.types[i].Non_effect,enemy_type))
            {
                return true;
            }
        }
        return false;
    }
    public static float TypeEffectiveness(Pokemon victim,Type enemy_type)
    {
        float effectiveness = 0;
        if (!isImmuneTo(victim, enemy_type))
        {
            if (victim.types[0].type_check(victim.types[0].weaknesses, enemy_type))
            {
                if (!victim.types[1].type_check(victim.types[1].Resistances, enemy_type) ||
                    victim.types[1].type_check(victim.types[1].weaknesses, enemy_type))
                    effectiveness = 4;
                if (victim.types[1].type_check(victim.types[1].Resistances, enemy_type))
                    effectiveness = 2;
            }
            if (victim.types[0].type_check(victim.types[0].Resistances, enemy_type))
            {
                if (victim.types[1].type_check(victim.types[1].Resistances, enemy_type))
                    effectiveness = 0.5f;
                if (victim.types[1].type_check(victim.types[1].weaknesses, enemy_type))
                    effectiveness = 2;
            }
        }
        else
            effectiveness = 0;
        Debug.Log(effectiveness);
        return effectiveness;
        
    }
}
