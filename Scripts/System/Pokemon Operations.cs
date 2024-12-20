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
        int part1 = rand.Next(0, 65536); // 16-bit random value
        int part2 = rand.Next(0, 65536); // 16-bit random value
        return math.abs((part1 << 16) | part2);
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
        int NatureValue = 0;
        if(new_pkm.Personality_value>0)
             NatureValue = new_pkm.Personality_value % 25;
        string[] natures =
        {
            "Hardy", "Lonely", "Brave", "Adamant", "Naughty",
            "Bold", "Docile", "Relaxed", "Impish", "Lax",
            "Timid", "Hasty", "Serious", "Jolly", "Naive",
            "Modest", "Mild", "Quiet", "Bashful", "Rash",
            "Calm", "Gentle", "Sassy", "Careful", "Quirky"
        };
        foreach (string nature in natures)
        {
            Nature n = Resources.Load<Nature>("Pokemon_project_assets/Pokemon_obj/Natures/" + nature);
            if (n.PValue == NatureValue)
            {
                new_pkm.nature = n;
                break;
            }
        }
    }

    public static void GetEV(string stat,float EVamount,Pokemon pkm)
    {
        switch (stat)
        {
            case "HP": 
                pkm.HP_EV=checkEV(pkm.HP_EV,EVamount,pkm);
                break;
            case "Attack": 
                pkm.Attack_EV=checkEV(pkm.Attack_EV,EVamount,pkm);
                break;
            case "Defense": 
                pkm.Defense_EV=checkEV(pkm.Defense_EV,EVamount,pkm);
                break;
            case "Special Attack": 
                pkm.SP_ATK_EV=checkEV(pkm.SP_ATK_EV,EVamount,pkm);
                break;
            case "Special Defense": 
                pkm.SP_DEF_EV=checkEV(pkm.SP_DEF_EV,EVamount,pkm);
                break;
            case "Speed": 
                pkm.speed_EV=checkEV(pkm.speed_EV,EVamount,pkm);
                break;
        }
    }
    static float checkEV(float ev,float amount,Pokemon pkm)
    {
        float sumEV = pkm.HP_EV + pkm.Attack_EV + pkm.Defense_EV + pkm.SP_ATK_EV + pkm.SP_DEF_EV + pkm.speed_EV;
        if (ev < 255 && sumEV < 510)
            return ev+=amount;
        return 0;
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
        int gender_check = 0;
        if(new_pkm.Personality_value>0)
            gender_check = new_pkm.Personality_value % 256;
        int pos = new_pkm.GenderRatio.IndexOf('/');
        string ratio = new_pkm.GenderRatio.Substring(pos + 1, new_pkm.GenderRatio.Length - pos - 1);
        float ratio_female = float.Parse(ratio);
        int Gender_threshold = math.abs((int)math.trunc(((ratio_female / 100) * 256)));
        if (gender_check < Gender_threshold)
            new_pkm.Gender = "Male";
        else
            new_pkm.Gender = "Female";
    }
    public static void SetPkmtraits(Pokemon new_pkm)
    {
        new_pkm.Pokemon_ID = Generate_ID(new_pkm.Pokemon_name);
        new_pkm.Personality_value = Generate_Personality();
        if(new_pkm.has_gender)
            get_Gender(new_pkm);
        getAbility(new_pkm);
        getNature(new_pkm);
        Generate_IVs(new_pkm);
    }
}
