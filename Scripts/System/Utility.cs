using UnityEngine;

public static class Utility
{
    public static int Get_rand(int min,int exclusive_lim)
    {
        return Random.Range(min, exclusive_lim);
    }
    public static bool isImmuneTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (t.type_check(t.Non_effect,enemy_type))
                return true;
        return false;
    } 
    static float isWeakTo(Pokemon victim,Type enemy_type)
    {
        float effectiveness = 0;
        foreach(Type t in victim.types)
            if (t.type_check(t.weaknesses, enemy_type))
                effectiveness += 2f;
        return effectiveness;
    }
    static float isResistantTo(Pokemon victim,Type enemy_type)
    {
        float effectiveness = 0;
        foreach(Type t in victim.types)
            if (t.type_check(t.Resistances, enemy_type))
                effectiveness -= 1f;
        return effectiveness;
    }
    public static float TypeEffectiveness(Pokemon victim,Type enemy_type)
    {
        float effectiveness = 0;
        if (isImmuneTo(victim, enemy_type))
        {
              effectiveness = 0;
              //Debug.Log(victim.Pokemon_name + " is immune to "+enemy_type.Type_name);
        }
        else
        {//check
            effectiveness += isWeakTo(victim, enemy_type);
            effectiveness += isResistantTo(victim, enemy_type);
        }
        return effectiveness;
    }
}
