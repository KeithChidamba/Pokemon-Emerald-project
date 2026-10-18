using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
[Serializable]
public struct CaptureInformation
{
    public string areaName;
    public int levelCaptured;
}
[CreateAssetMenu(fileName = "Pokemon", menuName = "Pokemon/Pokemon")]
public class Pokemon : ScriptableObject
{
    [FormerlySerializedAs("Base_Pokemon_name")] public string basePokemonName;
    public string pokemonName;
    public string nickName;
    public string pokemonDisplayName;
    
    [HideInInspector]public CaptureInformation captureInformation;
    
    public long pokemonID;
    public uint personalityValue;
    public bool isShiny;
    public Gender gender;
    public float ratioFemale;
    
    [FormerlySerializedAs("has_gender")] public bool hasGender = true;
    [FormerlySerializedAs("BaseHP")] public float baseHp;
    [FormerlySerializedAs("BaseAttack")] public float baseAttack;
    [FormerlySerializedAs("BaseDefense")] public float baseDefense;
    [FormerlySerializedAs("BaseSP_ATK")] public float baseSpecialAttack;
    [FormerlySerializedAs("BaseSP_DEF")] public float baseSpecialDefense;
    [FormerlySerializedAs("Basespeed")] public float baseSpeed;
    
    [FormerlySerializedAs("HP")] public float hp;
    [FormerlySerializedAs("max_HP")] public float maxHp;
    public float attack;
    public float defense;
    public float specialAttack;
    public float specialDefense;
    public float speed;
    public float hpIv;
    public float attackIv;
    public float defenseIv;
    public float specialAttackIv;
    public float specialDefenseIv;
    public float speedIv;
    public float hpEv;
    public float attackEv;
    public float defenseEv;
    public float specialAttackEv;
    public float specialDefenseEv;
    public float speedEv;
    [FormerlySerializedAs("EVs")] public List<EvYield> effortValues=new();
    
    [HideInInspector]public float accuracy = 100;
    [HideInInspector]public float evasion = 100;
    [HideInInspector]public float critChance = 6.25f;
    
    [FormerlySerializedAs("CatchRate")] public float catchRate;
    
    public int currentLevel = 1;
    public int currentExpAmount;
    public int currentLevelExpAmount;
    public int nextLevelExpAmount;
    [FormerlySerializedAs("EXPGroup")] public ExpGroup expGroup;
    [FormerlySerializedAs("exp_yield")] public int expYield;
    
    public int friendshipLevel;
    public bool hasTrainer;
    
    public List<PokemonTypeData> types = new();
    public StatusEffect statusEffect;
    public List<StatChangeData> statModifiers = new();
    
    [FormerlySerializedAs("evo_line")] public int[] evolutionLineLevels;
    public int currentEvolutionLineIndex;
    public FriendShipEvolutionData friendshipEvolutionRequirement;
    public bool requiresFriendshipEvolution;
    [FormerlySerializedAs("RequiresEvolutionStone")] public bool requiresEvolutionStone;
    [FormerlySerializedAs("EvolutionStoneName")] public EvolutionStone evolutionStone;
    [FormerlySerializedAs("split_evolution")] public bool splitEvolution;
    public List<Evolution> evolutions = new();
    
    public LearnSetMove[] learnSet;
    public List<MoveName> learnableTms = new();
    public List<MoveName> learnableHms = new();
    
    public List<Move> moveSet=new();
    public AbilityName[] abilities;
    public Ability ability;
    public Nature nature;
    
    public Item heldItem;
    public bool hasItem;
    
    [FormerlySerializedAs("front_picture")] public Sprite frontPicture;
    [FormerlySerializedAs("back_picture")] public Sprite backPicture;
    public Sprite partyFrame1;
    public Sprite partyFrame2;
    public Sprite battleIntroFrame;
    
    [HideInInspector]public string pokeballName;
    
    public void RemoveHeldItem()
    {
        heldItem = null;
        hasItem = false;
    }
    public void GiveItem(Item itemToGive)
    {
        hasItem = true;
        heldItem = InstanceFactory.CreateItem(itemToGive);
        heldItem.quantity = 1;
    }
    public bool HasType(PokemonType typeName)
    {
        return types.Any(type=>type.typeEnum == typeName);
    }
    
    private int ApplyFriendshipModifier(int currentIncrease)
    {
        float modifier = 1;
        
        if (hasItem)
        {
            var itemFriendshipMod = heldItem.GetDynamicModule<FriendshipModifierInfo>();
            if (itemFriendshipMod is not null)
            {
                modifier *= itemFriendshipMod.modifier;
            }
        }
        
        var pokeballItem = Resources.Load<Item>(DirectoryHandler.GetDirectory(AssetDirectory.Items) + pokeballName);
        
        var pokeballFriendshipMod = pokeballItem.GetDynamicModule<FriendshipModifierInfo>();
        
        if (pokeballFriendshipMod is not null)
        {
            modifier *= pokeballFriendshipMod.modifier;
        }
        
        return (int)math.ceil(currentIncrease * modifier);
    }
    
    /// <summary>
    /// Directly Change a pokemon's frienship level
    /// </summary>
    private void ChangeFriendshipLevel(int amount)
    {
        friendshipLevel = Math.Clamp(friendshipLevel + amount, 0, 255);
    }
    public void LoseFriendshipFromHerbs(Herb herbType)
    {
        var friendshipLoss = herbType switch
        {
            Herb.EnergyPowder => -5,
            Herb.EnergyRoot => -10,
            Herb.HealPowder => -5,
            Herb.RevivalHerb => -15,
            _ => 0
        };
        ChangeFriendshipLevel(friendshipLoss);
    }
    /// <summary>
    ///Change pokemon's friendship level and consider modifiers
    /// </summary>
    public void DetermineFriendshipLevelChange(FriendshipModifier action)
    {
        if (action == FriendshipModifier.CaughtPokemon)
        {
            ChangeFriendshipLevel(70);
        }
        if (action == FriendshipModifier.GiftPokemon)
        {
            ChangeFriendshipLevel( 120);
        }
        if (action == FriendshipModifier.XItem)
        {
            ChangeFriendshipLevel(ApplyFriendshipModifier(1));
        }
        if (action == FriendshipModifier.Fainted)
        {
            ChangeFriendshipLevel( friendshipLevel>99? -1 : -5 );
        }
        if (action == FriendshipModifier.LevelUp)
        {
            var isHighFriendship = friendshipLevel > 219;
            var increaseLimitLow = (isHighFriendship)? 1 : 3;
            var increaseLimitHigh = (isHighFriendship)? 3 : 6;
            var randomIncrease = Utility.RandomRange(increaseLimitLow, increaseLimitHigh);
            var amount = ApplyFriendshipModifier(randomIncrease);
            ChangeFriendshipLevel(amount);
        }
        
        if (action == FriendshipModifier.Vitamin)
        {
            var decreaseAmount = friendshipLevel switch
            {
                < 100 => -5,
                < 200 => -3,
                < 256 => -2,
                _ => 0
            };
            ChangeFriendshipLevel(decreaseAmount);
        }

        if (action == FriendshipModifier.Berry)
        {
            ChangeFriendshipLevel(ApplyFriendshipModifier(10) );
        }
    }
    
    public float AccountForExpGainModifier()
    {
        if (hasItem)
        {
            var expModInfo = heldItem.GetDynamicModule<ExpModifierInfo>();
            if (expModInfo is not null)
            {
                var hasExpGainBous = expModInfo.modifier == ExpModifier.ExpGainBonus;
                if (hasExpGainBous)
                {
                    return expModInfo.modifierFactor;
                }
            }
        }
        return 1f;
    }
    
    public int CalculateExperience()
    {
        var trainerBonus = hasTrainer? 1.5f : 1f;
        var baseExp = (expYield * currentLevel) / 7f;
        return (int)math.trunc(baseExp * trainerBonus);
    }

    public void IncreaseStats()
    {
        attack = DetermineStatIncrease(baseAttack,attackIv,attackEv,Stat.Attack);
        defense = DetermineStatIncrease(baseDefense,defenseIv,defenseEv,Stat.Defense);
        speed = DetermineStatIncrease(baseSpeed,speedIv,speedEv,Stat.Speed);
        specialAttack = DetermineStatIncrease(baseSpecialAttack,specialAttackIv,specialAttackEv,Stat.SpecialAttack);
        specialDefense = DetermineStatIncrease(baseSpecialDefense,specialDefenseIv,specialDefenseEv,Stat.SpecialDefense);
        maxHp = DetermineHealthIncrease();
    }
    float GetNatureModifier(Stat stat)
     {
         if (nature.statToIncrease == stat)
             return 1.1f;
         if (nature.statToDecrease == stat)
             return 0.9f;
         return 1;
     }
    float DetermineStatIncrease(float baseStat,float iv,float ev,Stat stat)
    {
        float brackets1 = (2*baseStat) + iv + (ev / 4);
        float bracket2 = currentLevel / 100f;
        return math.floor((brackets1 * bracket2 + 5f) * GetNatureModifier(stat));
    }
    float DetermineHealthIncrease()
    {
        float bracket1 = (2*baseHp) + hpIv + (hpEv / 4);
        float bracket2 = currentLevel / 100f;
        float bracket3 = currentLevel + 10f;
        return math.floor(bracket1 * bracket2 + bracket3);
    }

    public void Evolve(Evolution evo)
    {
        var previousName = pokemonName;

        pokemonName = evo.evolutionName;
        PokemonOperations.UpdatePokemonDisplayName(this,previousName);
        
        effortValues=evo.effortValues;
        types = evo.types;
        ability = evo.ability;
        
        learnableTms = new List<MoveName>(evo.learnableTms);
        learnableHms = new List<MoveName>(evo.learnableHms);
        
        learnSet = (LearnSetMove[])evo.learnSet.Clone();
        frontPicture = evo.frontPicture;
        backPicture = evo.backPicture;
        partyFrame1 = evo.partyFrame1;
        partyFrame2 = evo.partyFrame2;
        battleIntroFrame = evo.battleIntroFrame;
        expGroup = evo.expGroup;
        expYield = evo.expYield;
        catchRate = evo.catchRate;
        baseHp=evo.baseHp;
        baseAttack=evo.baseAttack;
        baseDefense=evo.baseDefense;
        baseSpecialAttack=evo.baseSpecialAttack;
        baseSpecialDefense=evo.baseSpecialDefense;
        baseSpeed = evo.baseSpeed;
        requiresFriendshipEvolution = evo.requiresFriendshipEvolution;
        requiresEvolutionStone = evo.requiresEvolutionStone;
        friendshipEvolutionRequirement = evo.friendshipEvolutionRequirement;
        currentEvolutionLineIndex++;
    }
}
