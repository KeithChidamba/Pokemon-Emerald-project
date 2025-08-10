using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;

public static class PokemonOperations
{
    public static bool LearningNewMove;
    public static bool SelectingMoveReplacement;
    public static Pokemon CurrentPokemon;
    public static Move NewMove;
    public static Action<bool> OnEvChange;
    public enum FriendshipModifier{Fainted,LevelUp,Vitamin,EvBerry}
    public enum StatusEffect{None,Paralysis,Burn,Poison,BadlyPoison,Freeze,Sleep,FullHeal}
    public enum Gender{None,Male,Female}
    public enum ExpGroup{Erratic,Fast,MediumFast,MediumSlow,Slow,Fluctuating}
    public enum Stat{None,Attack,Defense,SpecialAttack,SpecialDefense,Speed,Hp,Crit,Accuracy,Evasion}
    public enum Types
    {
        Normal, Fire, Water, Electric, Grass, Ice,
        Fighting, Poison, Ground, Flying, Psychic,
        Bug, Rock, Ghost, Dragon, Dark, Steel
    }
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
        var abilityEnum = (pokemon.abilities.Length > 1)? 
             pokemon.abilities[pokemon.personalityValue % 2] : pokemon.abilities[0];
        pokemon.abilityName = NameDB.GetAbility(abilityEnum);
        pokemon.ability = Resources.Load<Ability>(
            Save_manager.GetDirectory(Save_manager.AssetDirectory.Abilities)
            + pokemon.abilityName.ToLower());
    }
    public static bool ContainsType(Types[]typesList ,Type typesToCheck)
    {
        foreach (var type in typesList)
            if (type.ToString() == typesToCheck.typeName)
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
            var assignedNature = Resources.Load<Nature>(
                Save_manager.GetDirectory(Save_manager.AssetDirectory.Natures)
                + nature);
            if (assignedNature.requiredNatureValue != natureValue) continue;
            pokemon.nature = assignedNature;
            break;
        }
    }
    public static int CalculateExpForNextLevel(int currentLevel, ExpGroup expGroup)
    {
        if (currentLevel == 0) return 0;
        var cube = Utility.Cube(currentLevel);
        var square = Utility.Square(currentLevel);
        switch (expGroup)
        {
            case ExpGroup.Erratic: 
                return CalculateErratic(currentLevel);
            case ExpGroup.Fast: 
                return (int)math.trunc((4 * cube ) / 5f );
            case ExpGroup.MediumFast: 
                return cube;
            case ExpGroup.MediumSlow:
                return (int)math.trunc( ( (6 * cube ) / 5f) - (15 * square ) + (100 * currentLevel) - 140 );
            case ExpGroup.Slow:
                return (int)math.trunc((5 * cube ) / 4f);
            case ExpGroup.Fluctuating:
                return CalculateFluctuating(currentLevel);
            default:
                Debug.Log("pokemon has no exp group");
                break;
        }
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

    public static ref float GetEvStatRef(Stat stat, Pokemon pkm)
    {
        switch (stat)
        {
            case Stat.Hp: return ref pkm.hpEv;
            case Stat.Attack: return ref pkm.attackEv;
            case Stat.Defense: return ref pkm.defenseEv;
            case Stat.SpecialAttack: return ref pkm.specialAttackEv;
            case Stat.SpecialDefense: return ref pkm.specialDefenseEv;
            case Stat.Speed: return ref pkm.speedEv;
            default:
                throw new ArgumentException($"Invalid stat parsed: {stat}");
        }
    }
    public static void CalculateEvForStat(Stat stat,float evAmount,Pokemon pkm)
    {
        ref float evRef = ref GetEvStatRef(stat, pkm);
        evRef = CheckEvLimit(evRef,evAmount,pkm);
    }
    static float CheckEvLimit(float currentEv,float amount,Pokemon pokemon)
    {
        var totalEv = pokemon.hpEv + pokemon.attackEv + pokemon.defenseEv +
                        pokemon.specialAttackEv + pokemon.specialDefenseEv + pokemon.speedEv;

        // Prevent over-adding if already at cap
        if (amount > 0 && (currentEv >= 252 || totalEv >= 510))
        {
            OnEvChange?.Invoke(false);
            return currentEv;
        }

        // Calculate clamped new EV
        float maxAssignable = Math.Min(252 - currentEv, 510 - totalEv);
        float clampedAmount = Math.Clamp(amount, -currentEv, maxAssignable);
        bool changed = clampedAmount != 0;

        OnEvChange?.Invoke(changed);
        
        return currentEv + clampedAmount;
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
        if (CurrentPokemon.currentLevel > CurrentPokemon.learnSet[^1].requiredLevel)
        {//No more moves to learn
            LearningNewMove = false;
            return;
        }
        var counter = 0;
        var isPartyPokemon = Pokemon_party.Instance.party.Contains(CurrentPokemon);
        foreach (var move in CurrentPokemon.learnSet)
        {
            if (CurrentPokemon.currentLevel == move.requiredLevel)
            {
                var moveName = move.GetName().ToLower();
                LearnMove(moveName, isPartyPokemon);
                break;
            }
            counter++;
        }
        if (counter == CurrentPokemon.learnSet.Length)
            LearningNewMove = false;
    }

    private static void LearnMove(string moveName,bool isPartyPokemon = true)
    {
        var assetPath = Save_manager.GetDirectory(Save_manager.AssetDirectory.Moves) + moveName;
        
        var moveFromAsset = Resources.Load<Move>(assetPath);

        if (CurrentPokemon.moveSet.Count == 4) 
        {
            if (isPartyPokemon)
            {
                SelectingMoveReplacement = true;
                Dialogue_handler.Instance.DisplayList(
                    $"{CurrentPokemon.pokemonName} is trying to learn {moveName} ,do you want it to learn" +
                    $" {moveName}?", "", new[] { "LearnMove", "SkipMove" },
                    new[] { "Yes", "No" });
                NewMove = moveFromAsset;
            }
            //wild pokemon get generated with somewhat random moveset choices
            else
            {
                CurrentPokemon.moveSet[Utility.RandomRange(0, 4)]
                    = Obj_Instance.CreateMove(moveFromAsset);
                LearningNewMove = false;
            }
        }
        else
        {
            var newMove = Obj_Instance.CreateMove(moveFromAsset);
            if (CurrentPokemon.moveSet.Contains(newMove))
            {
                if (isPartyPokemon)
                {
                    Dialogue_handler.Instance.DisplayBattleInfo(
                        $"{CurrentPokemon.pokemonName} already knows {moveName}", true);
                }
                LearningNewMove = false;
                return;
            }
            if (isPartyPokemon)
            {
                Dialogue_handler.Instance.DisplayBattleInfo(
                    $"{CurrentPokemon.pokemonName} learned {moveName}",true);
            }
            CurrentPokemon.moveSet.Add(newMove);
            LearningNewMove = false;
        }
    }
    public static void LearnTmOrHm(AdditionalItemInfo itemInfo, Pokemon pokemon)
    {
        CurrentPokemon = pokemon;
        switch (itemInfo)
        {
            case TM tm:
                if (CurrentPokemon.learnableTms.Contains(tm.TmName))
                    LearnMove(tm.move.moveName);
                break;
            case HM hm:
                if (CurrentPokemon.learnableHms.Contains(hm.HmName))
                    LearnMove(hm.move.moveName);
                break;
        }
    }
    public static void LearnSelectedMove(int moveIndex)
    {
        Pokemon_Details.Instance.OnMoveSelected -= LearnSelectedMove;
        Pokemon_Details.Instance.learningMove = false;
        InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonDetails);
        Dialogue_handler.Instance.DisplayBattleInfo(CurrentPokemon.pokemonName + " forgot " 
            + CurrentPokemon.moveSet[moveIndex].moveName 
            + " and learned " + NewMove.moveName,false);
        CurrentPokemon.moveSet[moveIndex] = Obj_Instance.CreateMove(NewMove);
        SelectingMoveReplacement = false;
        LearningNewMove = false;
    }
    public static void UpdateHealthPhase(Pokemon pokemon,RawImage hpSliderColor)
    {
        List<(float threshold, Color color, int phase)> healthPhases = new()
        {
            (0.2f, Color.red, 3),
            (0.5f, Color.yellow, 2),
            (1f, Color.green, 1),
        };

        var hpPercentage = pokemon.hp / pokemon.maxHp;
        var phase = healthPhases.FirstOrDefault(hp => hpPercentage <= hp.threshold);

        hpSliderColor.color = phase.color;
        pokemon.healthPhase = phase.phase;
    }
    private static void AssignPokemonGender(Pokemon pokemon)
    {
        uint genderValue = 0;
        if(pokemon.personalityValue>0)
            genderValue = pokemon.personalityValue % 256;
        var genderThreshold = math.abs((int)math.trunc(((pokemon.ratioFemale / 100) * 256)));
        pokemon.gender = (genderValue < genderThreshold)? Gender.Male : Gender.Female;
    }
    public static void SetPokemonTraits(Pokemon newPokemon)
    {
        newPokemon.personalityValue = GeneratePersonalityValue();
        newPokemon.pokemonID = GeneratePokemonID(newPokemon);
        if (newPokemon.hasGender)
            AssignPokemonGender(newPokemon);
        else
            newPokemon.gender = Gender.None;
        
        AssignPokemonAbility(newPokemon);
        AssignPokemonNature(newPokemon);
        GeneratePokemonIVs(newPokemon);
    }
}
