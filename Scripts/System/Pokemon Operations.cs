using System.Linq;
using UnityEngine;
using Unity.Mathematics;

public static class PokemonOperations
{
    public static bool LearningNewMove = false;
    public static Pokemon CurrentPkm;
    public static Move NewMove;
    private static long GeneratePokemonID(Pokemon pokemon)//pokemon's unique ID
    {
        int combinedIDs = Game_Load.Instance.playerData.Trainer_ID;
        combinedIDs <<= 16;
        combinedIDs += Game_Load.Instance.playerData.Secret_ID;
        long pkmID = (((long)combinedIDs)<<32) | pokemon.Personality_value;
        return math.abs(pkmID);
    }
    private static uint GeneratePersonalityValue()
    {
        System.Random rand = new System.Random();
        int part1 = rand.Next(0, 65536); // 16-bit random value
        int part2 = rand.Next(0, 65536); // 16-bit random value
        return (uint)math.abs((part1 << 16) | part2);
    }
    private static void AssignPokemonAbility(Pokemon pokemon)
    {
        pokemon.ability = null;
        pokemon.ability_name = (pokemon.abilities.Length > 1)? 
             pokemon.abilities[pokemon.Personality_value % 2]
            :pokemon.abilities[0];
        pokemon.ability = Resources.Load<Ability>("Pokemon_project_assets/Pokemon_obj/Abilities/" + pokemon.ability_name.ToLower());
    }
    public static bool ContainsType(string[]typesList ,Type typeToCheck)
    {
        foreach (string type in typesList)
            if (type == typeToCheck.Type_name)
                return true;
        return false;
    }
    private static void AssignPokemonNature(Pokemon pokemon)
    {
        uint natureValue = 0;
        if (pokemon.Personality_value > 0)
            natureValue = pokemon.Personality_value % 25;
        string[] natures =
        {
            "Hardy", "Lonely", "Brave", "Adamant", "Naughty",
            "Bold", "Docile", "Relaxed", "Impish", "Lax",
            "Timid", "Hasty", "Serious", "Jolly", "Naive",
            "Modest", "Mild", "Quiet", "Bashful", "Rash",
            "Calm", "Gentle", "Sassy", "Careful", "Quirky"
        };
        foreach (var nature in natures)
        {
            var assignedNature = Resources.Load<Nature>("Pokemon_project_assets/Pokemon_obj/Natures/" + nature);
            if (assignedNature.requiredNatureValue != natureValue) continue;
            pokemon.nature = assignedNature;
            break;
        }
    }
    public static int CalculateExpForNextLevel(int currentLevel, string expGroup)
    {
        if (currentLevel == 0) return 0;
        var cube = Utility.Cube(currentLevel);
        var square = Utility.Square(currentLevel);
        switch (expGroup)
        {
            case "Erratic": 
                return CalculateErratic(currentLevel);
            case "Fast": 
                return (int)math.trunc((4 * cube ) / 5f );
            case "Medium Fast": 
                return cube;
            case "Medium Slow":
                return (int)math.trunc( ( (6 * cube ) / 5f) - (15 * square ) + (100 * currentLevel) - 140 );
            case "Slow":
                return (int)math.trunc((5 * cube ) / 4f);
            case "Fluctuating":
                return CalculateFluctuating(currentLevel);
        }Debug.Log("pokemon has no exp group");
        return 0;
    }
    private static int CalculateErratic(int level)
    {
        var cube = Utility.Cube(level);
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
    private static int CalculateFluctuating(int level)
    {
        var cube = Utility.Cube(level);
        if (0 < level & level <= 15)
            return (int)math.trunc( cube *  (24 + math.floor((level+1) / 3f) / 50f) );
        if (15 < level & level <= 36)
            return (int)math.trunc( cube *  ( (14 + level) / 50f) );
        if (36 < level & level <= 100)
            return (int)math.trunc( cube * ( (32 + math.floor(level/2f) ) / 50f) );
        return 0;
    }
    public static void CalculateEvForStat(string stat,float evAmount,Pokemon pkm)
    {
        switch (stat)
        {
            case "HP": 
                pkm.HP_EV=CheckEvLimit(pkm.HP_EV,evAmount,pkm);
                break;
            case "Attack": 
                pkm.Attack_EV=CheckEvLimit(pkm.Attack_EV,evAmount,pkm);
                break;
            case "Defense": 
                pkm.Defense_EV=CheckEvLimit(pkm.Defense_EV,evAmount,pkm);
                break;
            case "Special Attack": 
                pkm.SP_ATK_EV=CheckEvLimit(pkm.SP_ATK_EV,evAmount,pkm);
                break;
            case "Special Defense": 
                pkm.SP_DEF_EV=CheckEvLimit(pkm.SP_DEF_EV,evAmount,pkm);
                break;
            case "Speed": 
                pkm.speed_EV=CheckEvLimit(pkm.speed_EV,evAmount,pkm);
                break;
        }
    }
    static float CheckEvLimit(float ev,float amount,Pokemon pokemon)
    {
        var sumOfEvs = pokemon.HP_EV + pokemon.Attack_EV + pokemon.Defense_EV + pokemon.SP_ATK_EV + pokemon.SP_DEF_EV + pokemon.speed_EV;
        if (ev < 255 && sumOfEvs < 510)
            return ev+amount;
        return ev;
    }
    private static void GeneratePokemonIVs(Pokemon pokemon)
    {
        pokemon.HP_IV =  Utility.RandomRange(0,32);
        pokemon.Attack_IV = Utility.RandomRange(0,32);
        pokemon.Defense_IV =  Utility.RandomRange(0,32);
        pokemon.SP_ATK_IV =  Utility.RandomRange(0,32);
        pokemon.SP_DEF_IV =  Utility.RandomRange(0,32);
        pokemon.speed_IV =  Utility.RandomRange(0,32);
    }

    public static void GetNewMove(Pokemon pokemon)
    {
        LearningNewMove = true;
        CurrentPkm = pokemon;
        var counter = 0;
        foreach (var move in CurrentPkm.learnSet)
        {
            var requiredLevel = int.Parse(move.Substring(move.Length - 2, 2));
            if (CurrentPkm.Current_level == requiredLevel)
            {
                var pos = move.IndexOf('/')+1;
                var moveType = move.Substring(0, pos - 1).ToLower();
                var moveName = move.Substring(pos, move.Length - 2 - pos).ToLower();
                if (CurrentPkm.move_set.Count == 4) 
                {//leveling up from battle or rare candies
                    if(Options_manager.Instance.playerInBattle || Pokemon_party.instance.party.Contains(CurrentPkm))
                    {
                        Battle_handler.Instance.displayingInfo = true;
                        Dialogue_handler.instance.Write_Info(
                            CurrentPkm.Pokemon_name + " is trying to learn " + moveName + ", do you want it to learn " + moveName +
                            "?", "Options", "Learn_Move", "", "Skip_Move", "Yes", "No");
                        NewMove = Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + moveType + "/" + moveName);
                    }
                    //wild pokemon get generated with somewhat random moveset choices
                    else
                        CurrentPkm.move_set[Utility.RandomRange(0,4)] = Obj_Instance.set_move(Resources.Load<Move>(
                            "Pokemon_project_assets/Pokemon_obj/Moves/" + moveType + "/" + moveName));
                }
                else
                {
                    if(Options_manager.Instance.playerInBattle)
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
    public static void LearnSelectedMove(int moveIndex)
    {
        Pokemon_Details.instance.OnMoveSelected -= LearnSelectedMove;
        Pokemon_Details.instance.LearningMove = false;
        Pokemon_Details.instance.Exit_details();
        Dialogue_handler.instance.Battle_Info(CurrentPkm.Pokemon_name+" forgot "+CurrentPkm.move_set[moveIndex].Move_name+" and learned "+NewMove.Move_name);
        CurrentPkm.move_set[moveIndex] = Obj_Instance.set_move(NewMove);
        Battle_handler.Instance.levelUpQueue.RemoveAll(p=>p.pokemon==CurrentPkm);
        Turn_Based_Combat.Instance.levelEventDelay = false;
    }
    private static void AssignPokemonGender(Pokemon pokemon)
    {
        uint genderValue = 0;
        if(pokemon.Personality_value>0)
            genderValue = pokemon.Personality_value % 256;
        var pos = pokemon.GenderRatio.IndexOf('/');
        var ratio = pokemon.GenderRatio.Substring(pos + 1, pokemon.GenderRatio.Length - pos - 1);
        var ratioFemale = float.Parse(ratio);
        var genderThreshold = math.abs((int)math.trunc(((ratioFemale / 100) * 256)));
        pokemon.Gender = (genderValue < genderThreshold)? "Male" : "Female";
    }
    public static void SetPokemonTraits(Pokemon newPokemon)
    {
        newPokemon.Personality_value = GeneratePersonalityValue();
        newPokemon.Pokemon_ID = GeneratePokemonID(newPokemon);
        if(newPokemon.has_gender)
            AssignPokemonGender(newPokemon);
        AssignPokemonAbility(newPokemon);
        AssignPokemonNature(newPokemon);
        GeneratePokemonIVs(newPokemon);
    }
}
