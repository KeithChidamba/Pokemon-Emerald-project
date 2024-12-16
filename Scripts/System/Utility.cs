using UnityEngine;

public static class Utility
{
    private static float effectiveness = 0;
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
    static void isWeakTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (t.type_check(t.weaknesses, enemy_type))
            {
                //Debug.Log(victim.Pokemon_name + " is weak to "+enemy_type.Type_name);
                effectiveness *= 2f;
            }
    }
    static void isResistantTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (t.type_check(t.Resistances, enemy_type))
            {
                //Debug.Log(victim.Pokemon_name + " is resistant to "+enemy_type.Type_name);
                effectiveness /= 2f;
            }
    }

    public static bool is_Stab(Pokemon pkm,Type move)
    {
        foreach(Type t in pkm.types)
            if (t == move)
                return true;
        return false;
    }
    public static float TypeEffectiveness(Pokemon victim,Type enemy_type)
    {
        if (isImmuneTo(victim, enemy_type))
        {
              effectiveness = 0;
              //Debug.Log(victim.Pokemon_name + " is immune to "+enemy_type.Type_name);
        }
        else
        {
            effectiveness = 1;
            isWeakTo(victim, enemy_type);
            isResistantTo(victim, enemy_type);
        }
        return effectiveness;
    }
}
