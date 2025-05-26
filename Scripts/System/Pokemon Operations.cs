using System.Linq;
using UnityEngine;
using Unity.Mathematics;

public static class PokemonOperations
{
    public static bool LearningNewMove;
    public static bool SelectingMoveReplacement;
    public static Pokemon CurrentPokemon;
    public static Move NewMove;
    private static long GeneratePokemonID(Pokemon pokemon)//pokemon's unique ID
    {
        int combinedIDs = Game_Load.Instance.playerData.trainerID;
        combinedIDs <<= 16;
        combinedIDs += Game_Load.Instance.playerData.secretID;
        long pkmID = (((long)combinedIDs)<<32) | pokemon.personalityValue;
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
        pokemon.abilityName = (pokemon.abilities.Length > 1)? 
             pokemon.abilities[pokemon.personalityValue % 2]
            :pokemon.abilities[0];
        pokemon.ability = Resources.Load<Ability>("Pokemon_project_assets/Pokemon_obj/Abilities/" + pokemon.abilityName.ToLower());
    }
    public static bool ContainsType(string[]typesList ,Type typeToCheck)
    {
        foreach (string type in typesList)
            if (type == typeToCheck.typeName)
                return true;
        return false;
    }
    private static void AssignPokemonNature(Pokemon pokemon)
    {
        uint natureValue = 0;
        if (pokemon.personalityValue > 0)
            natureValue = pokemon.personalityValue % 25;
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
                pkm.hpEv=CheckEvLimit(pkm.hpEv,evAmount,pkm);
                break;
            case "Attack": 
                pkm.attackEv=CheckEvLimit(pkm.attackEv,evAmount,pkm);
                break;
            case "Defense": 
                pkm.defenseEv=CheckEvLimit(pkm.defenseEv,evAmount,pkm);
                break;
            case "Special Attack": 
                pkm.specialAttackEv=CheckEvLimit(pkm.specialAttackEv,evAmount,pkm);
                break;
            case "Special Defense": 
                pkm.specialDefenseEv=CheckEvLimit(pkm.specialDefenseEv,evAmount,pkm);
                break;
            case "Speed": 
                pkm.speedEv=CheckEvLimit(pkm.speedEv,evAmount,pkm);
                break;
        }
    }
    static float CheckEvLimit(float ev,float amount,Pokemon pokemon)
    {
        if (ev >= 252) return ev;
        var sumOfEvs = pokemon.hpEv + pokemon.attackEv + pokemon.defenseEv + pokemon.specialAttackEv + pokemon.specialDefenseEv + pokemon.speedEv;
        if (ev + amount >= 252) amount = (ev + amount)-252;
        if (sumOfEvs < 510) return ev+amount;
        return ev;
    }
    private static void GeneratePokemonIVs(Pokemon pokemon)
    {
        pokemon.hpIv =  Utility.RandomRange(0,32);
        pokemon.attackIv = Utility.RandomRange(0,32);
        pokemon.defenseIv =  Utility.RandomRange(0,32);
        pokemon.specialAttackIv =  Utility.RandomRange(0,32);
        pokemon.specialDefenseIv =  Utility.RandomRange(0,32);
        pokemon.speedIv =  Utility.RandomRange(0,32);
    }

    public static void CheckForNewMove(Pokemon pokemon)
    {
        LearningNewMove = true;
        CurrentPokemon = pokemon;
        var counter = 0;
        var isPartyPokemon = Pokemon_party.Instance.party.Contains(CurrentPokemon);
        var inBattle = Options_manager.Instance.playerInBattle;
        foreach (var move in CurrentPokemon.learnSet)
        {
            var requiredLevel = int.Parse(move.Substring(move.Length - 2, 2));
            if (CurrentPokemon.currentLevel == requiredLevel)
            {
                var pos = move.IndexOf('/')+1;
                var moveType = move.Substring(0, pos - 1).ToLower();
                var moveName = move.Substring(pos, move.Length - 2 - pos).ToLower();
                if(!inBattle & isPartyPokemon)
                    Game_ui_manager.Instance.canExitParty = false;
                
                if (CurrentPokemon.moveSet.Count == 4) 
                {//leveling up from battle or rare candies
                    if(inBattle || isPartyPokemon)
                    {
                        SelectingMoveReplacement = true;
                        Battle_handler.Instance.displayingInfo = inBattle;
                        Dialogue_handler.Instance.DisplayList(
                            $"{CurrentPokemon.pokemonName} is trying to learn {moveName} ,do you want it to learn {moveName}?",
                            "", new[]{ "LearnMove","SkipMove" }, new[]{"Yes", "No"});
                        NewMove = Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + moveType + "/" + moveName);
                    }
                    //wild pokemon get generated with somewhat random moveset choices
                    else
                        CurrentPokemon.moveSet[Utility.RandomRange(0,4)] = Obj_Instance.CreateMove(
                            Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + moveType + "/" + moveName));
                }
                else
                {
                    if (isPartyPokemon)
                    {
                        if(!inBattle)
                            Game_ui_manager.Instance.canExitParty = true;
                        Dialogue_handler.Instance.DisplayBattleInfo(CurrentPokemon.pokemonName+" learned "+moveName,true);
                    }
                    var newMove = Obj_Instance.CreateMove(Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + moveType + "/" + moveName));
                    if (!CurrentPokemon.moveSet.Contains(newMove))
                        CurrentPokemon.moveSet.Add(newMove);
                    LearningNewMove = false;
                }
                break;
            }
            counter++;
        }
        if (counter == CurrentPokemon.learnSet.Length)
        {
            if (isPartyPokemon & !inBattle)
                Game_ui_manager.Instance.canExitParty = true;
            LearningNewMove = false;
        }
    }
    public static void LearnSelectedMove(int moveIndex)
    {
        Pokemon_Details.Instance.OnMoveSelected -= LearnSelectedMove;
        Pokemon_Details.Instance.learningMove = false;
        Pokemon_Details.Instance.ExitDetails();
        Dialogue_handler.Instance.DisplayBattleInfo(CurrentPokemon.pokemonName + " forgot " 
            + CurrentPokemon.moveSet[moveIndex].moveName 
            + " and learned " + NewMove.moveName,true);
        CurrentPokemon.moveSet[moveIndex] = Obj_Instance.CreateMove(NewMove);
        SelectingMoveReplacement = false;
        LearningNewMove = false;
        Game_ui_manager.Instance.canExitParty = true;
    }
    private static void AssignPokemonGender(Pokemon pokemon)
    {
        uint genderValue = 0;
        if(pokemon.personalityValue>0)
            genderValue = pokemon.personalityValue % 256;
        var pos = pokemon.genderRatio.IndexOf('/');
        var ratio = pokemon.genderRatio.Substring(pos + 1, pokemon.genderRatio.Length - pos - 1);
        var ratioFemale = float.Parse(ratio);
        var genderThreshold = math.abs((int)math.trunc(((ratioFemale / 100) * 256)));
        pokemon.gender = (genderValue < genderThreshold)? "Male" : "Female";
    }
    public static void SetPokemonTraits(Pokemon newPokemon)
    {
        newPokemon.personalityValue = GeneratePersonalityValue();
        newPokemon.pokemonID = GeneratePokemonID(newPokemon);
        if(newPokemon.hasGender)
            AssignPokemonGender(newPokemon);
        AssignPokemonAbility(newPokemon);
        AssignPokemonNature(newPokemon);
        GeneratePokemonIVs(newPokemon);
    }
}
