using System.Linq;
using UnityEngine;
using Unity.Mathematics;

public static class PokemonOperations
{
    public static bool LearningNewMove = false;
    public static Pokemon CurrentPkm;
    public static Move NewMove;
    private static long Generate_ID(Pokemon pkm)//pokemon's unique ID
    {
        int CombinedIDs = Game_Load.instance.player_data.Trainer_ID;
        CombinedIDs <<= 16;
        CombinedIDs += Game_Load.instance.player_data.Secret_ID;
        long pkmID = (((long)CombinedIDs)<<32) | pkm.Personality_value;
        return math.abs(pkmID);
    }
    private static uint Generate_Personality()
    {
        System.Random rand = new System.Random();
        int part1 = rand.Next(0, 65536); // 16-bit random value
        int part2 = rand.Next(0, 65536); // 16-bit random value
        return (uint)math.abs((part1 << 16) | part2);
    }
    private static void getAbility(Pokemon new_pkm)
    {
        new_pkm.ability = null;
        if (new_pkm.abilities.Length > 1)
            new_pkm.ability_name = new_pkm.abilities[new_pkm.Personality_value % 2];
        else
            new_pkm.ability_name = new_pkm.abilities[0];
        new_pkm.ability = Resources.Load<Ability>("Pokemon_project_assets/Pokemon_obj/Abilities/" + new_pkm.ability_name.ToLower());
    }

    private static void getNature(Pokemon new_pkm)
    {
        uint NatureValue = 0;
        if (new_pkm.Personality_value > 0)
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
    public static int GetNextLv(int Current_level, string EXPGroup)
    {
        if (Current_level == 0) return 0;
        int cube = Utility.Cube(Current_level);
        int square = Utility.Square(Current_level);
        switch (EXPGroup)
        {
            case "Erratic": 
                return Calc_Erratic(Current_level);
            case "Fast": 
                return (int)math.trunc((4 * cube ) / 5f );
            case "Medium Fast": 
                return cube;
            case "Medium Slow":
                return (int)math.trunc( ( (6 * cube ) / 5f) - (15 * square ) + (100 * Current_level) - 140 );
            case "Slow":
                return (int)math.trunc((5 * cube ) / 4f);
            case "Fluctuating":
                return CalcFluctuating(Current_level);
        }Debug.Log("pokemon has no exp group");
        return 0;
    }
    static int Calc_Erratic(int level)
    {
        int cube = Utility.Cube(level);
        if (0 < level & level <= 50)
            return (int)math.trunc( (cube * (100 - level)) / 50f );
        if (50 < level & level <= 68)
            return (int)math.trunc( ( cube * (150 - level)) / 100f );
        if (68 < level & level <= 98)
            return (int)math.trunc( ( cube * (1911 - (10*level) ) ) / 1500f );
        if (98 < level & level <= 100)
            return (int)math.trunc( ( cube * (160 - level)) / 100f );
        return 0;
    }
    static int CalcFluctuating(int level)
    {
        int cube = Utility.Cube(level);
        if (0 < level & level <= 15)
            return (int)math.trunc( cube *  (24 + math.floor((level+1) / 3f) / 50f) );
        if (15 < level & level <= 36)
            return (int)math.trunc( cube *  ( (14 + level) / 50f) );
        if (36 < level & level <= 100)
            return (int)math.trunc( cube * ( (32 + math.floor(level/2f) ) / 50f) );
        return 0;
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
            return ev+amount;
        return 0;
    }
    private static void Generate_IVs(Pokemon new_pkm)
    {
        new_pkm.HP_IV =  Utility.Get_rand(0,32);
        new_pkm.Attack_IV = Utility.Get_rand(0,32);
        new_pkm.Defense_IV =  Utility.Get_rand(0,32);
        new_pkm.SP_ATK_IV =  Utility.Get_rand(0,32);
        new_pkm.SP_DEF_IV =  Utility.Get_rand(0,32);
        new_pkm.speed_IV =  Utility.Get_rand(0,32);
    }

    public static void GetNewMove(Pokemon pkm)
    {
        LearningNewMove = true;
        CurrentPkm = pkm;
        int counter = 0;
        foreach (string move in CurrentPkm.learnSet)
        {
            int requiredLevel = int.Parse(move.Substring(move.Length - 2, 2));
            if (CurrentPkm.Current_level == requiredLevel)
            {
                int pos = move.IndexOf('/')+1;
                string moveType = move.Substring(0, pos - 1).ToLower();
                string moveName = move.Substring(pos, move.Length - 2 - pos).ToLower();
                if (CurrentPkm.move_set.Count == 4) 
                {//leveling up from battle or rare candies
                    if(Options_manager.instance.playerInBattle || Pokemon_party.instance.party.Contains(CurrentPkm))
                    {
                        Battle_handler.instance.displaying_info = true;
                        Dialogue_handler.instance.Write_Info(
                            CurrentPkm.Pokemon_name + " is trying to learn " + moveName + ", do you want it to learn " + moveName +
                            "?", "Options", "Learn_Move", "", "Skip_Move", "Yes", "No");
                        NewMove = Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + moveType + "/" + moveName);
                    }
                    //wild pokemon get generated with somewhat random moveset choices
                    else
                        CurrentPkm.move_set[Utility.Get_rand(0,4)] = Obj_Instance.set_move(Resources.Load<Move>(
                            "Pokemon_project_assets/Pokemon_obj/Moves/" + moveType + "/" + moveName));
                }
                else
                {
                    if(Options_manager.instance.playerInBattle)
                        Dialogue_handler.instance.Battle_Info(CurrentPkm.Pokemon_name+" learned "+moveName);
                    CurrentPkm.move_set.Add(Obj_Instance.set_move(Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + moveType + "/" + moveName)));
                    LearningNewMove = false;
                }
                break;
            }
            counter++;
        }

        if (counter == CurrentPkm.learnSet.Length)
            LearningNewMove = false;
    }
    public static void Learn_move(int index)
    {
        Pokemon_Details.instance.LearningMove = false;
        Pokemon_Details.instance.Exit_details();
        Dialogue_handler.instance.Battle_Info(CurrentPkm.Pokemon_name+" forgot "+CurrentPkm.move_set[index].Move_name+" and learned "+NewMove.Move_name);
        CurrentPkm.move_set[index] = Obj_Instance.set_move(NewMove);
        Battle_handler.instance.levelUpQueue.RemoveAll(p=>p.pokemon==CurrentPkm);
        Turn_Based_Combat.instance.LevelEventDelay = false;
    }
    private static void get_Gender(Pokemon new_pkm)
    {
        uint gender_check = 0;
        if(new_pkm.Personality_value>0)
            gender_check = new_pkm.Personality_value % 256;
        int pos = new_pkm.GenderRatio.IndexOf('/');
        string ratio = new_pkm.GenderRatio.Substring(pos + 1, new_pkm.GenderRatio.Length - pos - 1);
        Debug.Log("ratio: "+ratio);
        float ratio_female = float.Parse(ratio);
        int Gender_threshold = math.abs((int)math.trunc(((ratio_female / 100) * 256)));
        if (gender_check < Gender_threshold)
            new_pkm.Gender = "Male";
        else
            new_pkm.Gender = "Female";
    }
    public static void SetPkmtraits(Pokemon new_pkm)
    {
        new_pkm.Personality_value = Generate_Personality();
        new_pkm.Pokemon_ID = Generate_ID(new_pkm);
        if(new_pkm.has_gender)
            get_Gender(new_pkm);
        getAbility(new_pkm);
        getNature(new_pkm);
        Generate_IVs(new_pkm);
    }
}
