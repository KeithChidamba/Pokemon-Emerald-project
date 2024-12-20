using UnityEngine;
using Unity.Mathematics;
public static class PokemonOperations
{
    public static string Generate_ID(string name_)//pokemon's unique ID
    {
        int rand = Utility.Get_rand(0,name_.Length);
        string end_digits = Utility.Get_rand(0,name_.Length).ToString() + Utility.Get_rand(0,name_.Length).ToString() + Utility.Get_rand(0,name_.Length).ToString() + Utility.Get_rand(0,name_.Length).ToString();
        string id = rand.ToString();
        id += name_[rand];
        if (rand >= name_.Length-1)
            id += name_.Substring(rand-4, 3);
        else
            id += name_.Substring(rand, (name_.Length-1)-rand );
        id += end_digits;
        return id;
    }
    public static int Generate_Personality()
    {
        System.Random rand = new System.Random();
        int randomInt = rand.Next(int.MinValue, int.MaxValue);
        return randomInt;
    }
    public static void getAbility(Pokemon new_pkm)
    {
        new_pkm.ability = null;
        if (new_pkm.abilities.Length > 1)
        {
            if (new_pkm.Personality_value % 2 == 0)
                new_pkm.ability_name = new_pkm.abilities[0];
            else if (new_pkm.Personality_value % 2 == 1)
                new_pkm.ability_name = new_pkm.abilities[1];
        }
        else
            new_pkm.ability_name = new_pkm.abilities[0];
        new_pkm.ability = Resources.Load<Ability>("Pokemon_project_assets/Pokemon_obj/Abilities/" + new_pkm.ability_name.ToLower());
    }

    public static void getNature(Pokemon new_pkm)
    {
        int NatureValue = new_pkm.Personality_value % 25;
        string[] natures =
        {
            "Hardy", "Lonely", "Brave", "Adamant", "Naughty",
            "Bold", "Docile", "Relaxed", "Impish", "Lax",
            "Timid", "Hasty", "Serious", "Jolly", "Naive",
            "Modest", "Mild", "Quiet", "Bashful", "Rash",
            "Calm", "Gentle", "Sassy", "Careful", "Quirky"
        };
        Nature n=null;
        foreach (string nature in natures)
            n = Resources.Load<Nature>("Pokemon_project_assets/Pokemon_obj/Natures/" + nature);
        if (n?.PValue == NatureValue)
            new_pkm.nature = n;
    }

    public static void Generate_IVs(Pokemon new_pkm)
    {
        new_pkm.HP_IV =  Utility.Get_rand(0,32);
        new_pkm.Attack_IV = Utility.Get_rand(0,32);
        new_pkm.Defense_IV =  Utility.Get_rand(0,32);
        new_pkm.SP_ATK_IV =  Utility.Get_rand(0,32);
        new_pkm.SP_DEF_IV =  Utility.Get_rand(0,32);
        new_pkm.speed_IV =  Utility.Get_rand(0,32);
    }
    public static void get_Gender(Pokemon new_pkm)
    {
        int gender_check = new_pkm.Personality_value % 256;
        int pos = new_pkm.GenderRatio.IndexOf('/');
        int ratio_female = int.Parse(new_pkm.GenderRatio.Substring(pos + 1, new_pkm.GenderRatio.Length - pos - 1));
        int Gender_threshold = (int)math.trunc(((ratio_female / 100) * 256));
        Debug.Log("gender: "+gender_check +"/"+Gender_threshold);
        if (gender_check < Gender_threshold)
            new_pkm.Gender = "Male";
        else
            new_pkm.Gender = "Female";
    }
}
