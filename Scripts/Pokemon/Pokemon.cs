using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Pokemon", menuName = "pkm")]
public class Pokemon : ScriptableObject
{
    [FormerlySerializedAs("Base_Pokemon_name")] public string basePokemonName;
    [FormerlySerializedAs("Pokemon_name")] public string pokemonName;
    [FormerlySerializedAs("Pokemon_ID")] public long pokemonID = 0;
    [FormerlySerializedAs("Personality_value")] public uint personalityValue;
    [FormerlySerializedAs("Gender")] public string gender = "None";
    [FormerlySerializedAs("GenderRatio")] public string genderRatio = "50/50";
    public Nature nature;
    [FormerlySerializedAs("has_gender")] public bool hasGender = true;
    [FormerlySerializedAs("HP")] public float hp;
    [FormerlySerializedAs("max_HP")] public float maxHp;
    [FormerlySerializedAs("BaseHP")] public float baseHp;
    [FormerlySerializedAs("BaseAttack")] public float baseAttack;
    [FormerlySerializedAs("BaseDefense")] public float baseDefense;
    [FormerlySerializedAs("BaseSP_ATK")] public float baseSpecialAttack;
    [FormerlySerializedAs("BaseSP_DEF")] public float baseSpecialDefense;
    [FormerlySerializedAs("Basespeed")] public float baseSpeed;
    [FormerlySerializedAs("Attack")] public float attack;
    [FormerlySerializedAs("Defense")] public float defense;
    [FormerlySerializedAs("SP_ATK")] public float specialAttack;
    [FormerlySerializedAs("SP_DEF")] public float specialDefense;
    public float speed;
    [FormerlySerializedAs("HP_IV")] public float hpIv;
    [FormerlySerializedAs("Attack_IV")] public float attackIv;
    [FormerlySerializedAs("Defense_IV")] public float defenseIv;
    [FormerlySerializedAs("SP_ATK_IV")] public float specialAttackIv;
    [FormerlySerializedAs("SP_DEF_IV")] public float specialDefenseIv;
    [FormerlySerializedAs("speed_IV")] public float speedIv;
    [FormerlySerializedAs("HP_EV")] public float hpEv=0;
    [FormerlySerializedAs("Attack_EV")] public float attackEv=0;
    [FormerlySerializedAs("Defense_EV")] public float defenseEv=0;
    [FormerlySerializedAs("SP_ATK_EV")] public float specialAttackEv=0;
    [FormerlySerializedAs("SP_DEF_EV")] public float specialDefenseEv=0;
    [FormerlySerializedAs("speed_EV")] public float speedEv=0;
    [FormerlySerializedAs("EVs")] public List<string> effortValues=new();
    [FormerlySerializedAs("Accuracy")] public float accuracy = 100;
    [FormerlySerializedAs("Evasion")] public float evasion = 100;
    [FormerlySerializedAs("crit_chance")] public float critChance = 6.25f;
    [FormerlySerializedAs("CatchRate")] public float catchRate = 0;
    [FormerlySerializedAs("Current_level")] public int currentLevel = 1;
    [FormerlySerializedAs("CurrentExpAmount")] public int currentExpAmount = 0;
    [FormerlySerializedAs("NextLevelExpAmount")] public float nextLevelExpAmount = 0;
    [FormerlySerializedAs("EXPGroup")] public string expGroup = "";
    [FormerlySerializedAs("exp_yield")] public int expYield=0;
    public int friendshipLevel = 0;
    [FormerlySerializedAs("has_trainer")] public bool hasTrainer=false;
    public bool canAttack = true;
    public bool isFlinched = false;
    [FormerlySerializedAs("CanBeDamaged")] public bool canBeDamaged = true;
    public bool immuneToStatReduction = false;
    public List<Type> types;
    [FormerlySerializedAs("Status_effect")] public string statusEffect = "None";
    [FormerlySerializedAs("Buff_Debuffs")] public List<Buff_Debuff> buffAndDebuffs = new();
    [FormerlySerializedAs("evo_line")] public string[] evolutionLineLevels;
    public string friendshipEvolutionRequirement;
    [FormerlySerializedAs("RequiresEvolutionStone")] public bool requiresEvolutionStone = false;
    [FormerlySerializedAs("EvolutionStoneName")] public string evolutionStoneName = "None";
    public string[] abilities;
    [FormerlySerializedAs("split_evolution")] public bool splitEvolution = false;
    public bool requiresFriendshipEvolution = false;
    public string[] learnSet;
    [FormerlySerializedAs("move_set")] public List<Move> moveSet=new();
    public Ability ability;
    public List<Evolution> evolutions;
    [FormerlySerializedAs("HeldItem")] public Item heldItem;
    [FormerlySerializedAs("HasItem")] public bool hasItem = false;
    [FormerlySerializedAs("front_picture")] public Sprite frontPicture;
    [FormerlySerializedAs("back_picture")] public Sprite backPicture;
    public string pokeballName;
    public event Action OnNewLevel;
    public event Action<Pokemon> OnLevelUp;
    public event Action OnDamageTaken;
    //data conversion when json to obj
    public string abilityName;
    public string natureName;
    public List<string> evolutionData=new();
    public List<string> typeData=new();
    public List<string> moveData=new();
    public List<string> movePpData=new();
    
    public void SaveUnserializableData()
    {
        abilityName = ability.abilityName;
        natureName = nature.natureName;
        hasItem = (heldItem != null);
        moveData.Clear();
        typeData.Clear();
        movePpData.Clear();
        evolutionData.Clear();
        foreach (var move in moveSet)
        {
            moveData.Add(move.moveName + "/" + move.type.typeName);
            movePpData.Add(move.powerpoints+"/"+move.maxPowerpoints);
        }
        foreach (Type t in types)
            typeData.Add(t.typeName);
        foreach (Evolution e in evolutions)
            evolutionData.Add(e.evolutionName);
    }
    public void LoadUnserializedData()//gives values to attributes that cant be deserialized, using saved values
    {
        frontPicture = Testing.CheckImage("Pokemon_project_assets/pokemon_img/",pokemonName);
        backPicture = Testing.CheckImage("Pokemon_project_assets/pokemon_img/",pokemonName + "_b");
        nature = Resources.Load<Nature>("Pokemon_project_assets/Pokemon_obj/Natures/" + natureName.ToLower());
        ability = Resources.Load<Ability>("Pokemon_project_assets/Pokemon_obj/Abilities/" + abilityName.ToLower());
        moveSet.Clear();
        types.Clear();
        evolutions.Clear();
        for (int i = 0; i < moveData.Count; i++)
        {
            var splitPos1 = moveData[i].IndexOf('/')+1;
            var moveName = moveData[i].Substring(0, splitPos1 - 1).ToLower();
            var moveType = moveData[i].Substring(splitPos1,moveData[i].Length - splitPos1).ToLower();
            var moveCopy = Obj_Instance.CreateMove(Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + moveType + "/" + moveName));
            var splitPos2 = movePpData[i].IndexOf('/')+1;
            moveCopy.powerpoints = int.Parse(movePpData[i].Substring(0, splitPos2-1));
            moveCopy.maxPowerpoints = int.Parse(movePpData[i].Substring(splitPos2, movePpData[i].Length - splitPos2));
            moveSet.Add(moveCopy);
        }
        foreach (String t in typeData)
            types.Add(Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/" + t.ToLower()));
        foreach (String e in evolutionData)
             evolutions.Add(Resources.Load<Evolution>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + basePokemonName + "/" +e));
        for(int i =0; i < types.Count; i++)
            types[i].typeImage = Resources.Load<Sprite>("Pokemon_project_assets/ui/" + typeData[i].ToLower());
        Battle_handler.Instance.OnBattleEnd += ClearEvents;
    }

    public void RemoveHeldItem()
    {
        heldItem = null;
        hasItem = false;
    }

    public void GiveItem(Item itemToGive)
    {
        heldItem = Obj_Instance.CreateItem(itemToGive);
        heldItem.quantity = 1;
        hasItem = true;
    }
    public bool HasType(string typeName)
    {
        return types.Any(type => type.typeName == typeName);
    }

    private int ApplyFriendshipModifier(int currentIncrease)
    {
        float modifier = 1f;
        if (pokeballName == "Luxury Ball")
            modifier *= 1.5f;
        
        if (hasItem)//will throw error if check item name on null helditem
            if(heldItem.itemName == "Soothe Bell")
                modifier *= 1.5f;
        return (int)math.ceil(currentIncrease * modifier);
    }

    public void ChangeFriendshipLevel(int amount)
    {
        friendshipLevel = Math.Clamp(friendshipLevel + amount, 0, 255);
    }
    public void DetermineFriendshipLevelChange(bool isIncreasing, string action)
    {
        if (friendshipLevel > 254 && isIncreasing) return;
        
        var isHighFriendship = friendshipLevel > 219;
        
        if (action == "Fainted") ChangeFriendshipLevel( friendshipLevel>99? -1 : -5 );
        
        if (action == "Level Up")
        {
            var increaseLimitLow = (isHighFriendship)? 1 : 3;
            var increaseLimitHigh = (isHighFriendship)? 3 : 6;
            var randomIncrease = Utility.RandomRange(increaseLimitLow, increaseLimitHigh);
            var amount = ApplyFriendshipModifier(randomIncrease);
            ChangeFriendshipLevel( isIncreasing?  amount : -amount );
        }
        
        if (action == "Vitamin")
        {
            var increaseAmount = friendshipLevel switch
            {
                < 100 => 5,
                < 200 => 3,
                < 255 => 2,
                _ => 0
            };
            var amount = ApplyFriendshipModifier(increaseAmount);
            ChangeFriendshipLevel(isIncreasing?  amount: -amount );
        }
        
        if (action == "EV Berry") ChangeFriendshipLevel(ApplyFriendshipModifier(10) );
    }
    public void CheckEvolutionRequirements(int evoIndex)
    {
        if (requiresEvolutionStone)
        {
            Evolve(evolutions[evoIndex]); return;
        }

        if (requiresFriendshipEvolution)
        {
            CheckFriendshipEvolution(); return;
        }
        
        //regular evolution
        for (int i = 0; i < evolutionLineLevels.Length; i++)
        {
            var requiredLevelToEvolve = int.Parse(evolutionLineLevels[i]);
            if (currentLevel == requiredLevelToEvolve)
            {
                Evolve(evolutions[i+evoIndex]);
                break;
            }
        }
    }

    void CheckFriendshipEvolution()
    {
        var evolutionIndex = int.Parse(friendshipEvolutionRequirement.Split("/")[1]);
        var friendshipRequirement = int.Parse(friendshipEvolutionRequirement.Split("/")[0]);
        if (friendshipLevel >= friendshipRequirement)
            Evolve(evolutions[evolutionIndex]);
    }
    private void DetermineSplitEvolution()
    {
        int evolutionValue = (int)personalityValue % 10;
        if (evolutionValue>=0 & evolutionValue<5)
            CheckEvolutionRequirements(0);
        else if (evolutionValue>4 & evolutionValue<10)
            CheckEvolutionRequirements(2);
    }
    public void ReceiveExperience(int amount)
    {
        if (currentLevel > 99) return;
        currentExpAmount += amount;
        nextLevelExpAmount = PokemonOperations.CalculateExpForNextLevel(currentLevel,expGroup);
        if(currentExpAmount>nextLevelExpAmount)
            LevelUp();
    }
    public int CalculateExperience(Pokemon enemy)
    {
        var trainerBonus = 1f;
        var baseExp = (enemy.expYield*enemy.currentLevel) / 7f;
        var expItemBonus = 1f;
        if (hasItem)
            if (heldItem.itemType == "Exp Gain")//lucky egg
                expItemBonus = float.Parse(heldItem.itemEffect);
        if (enemy.hasTrainer)
            trainerBonus = 1.5f;
        return (int)math.trunc(baseExp * trainerBonus * expItemBonus);
    }
    void Evolve(Evolution evo)
    {
        pokemonName = evo.evolutionName;
        effortValues=evo.effortValues;
        types = evo.types;
        ability = evo.ability;
        learnSet = evo.learnSet;
        frontPicture = evo.frontPicture;
        backPicture = evo.backPicture;
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
        Pokemon_party.Instance.RefreshMemberCards();
    }

    void IncreaseStats()
    {
        attack = DetermineStatIncrease(baseAttack,attackIv,attackEv,"Attack");
        defense = DetermineStatIncrease(baseDefense,defenseIv,defenseEv,"Defense");
        speed = DetermineStatIncrease(baseSpeed,speedIv,speedEv,"Speed");
        specialAttack = DetermineStatIncrease(baseSpecialAttack,specialAttackIv,specialAttackEv,"Special Attack");
        specialDefense = DetermineStatIncrease(baseSpecialDefense,specialDefenseIv,specialDefenseEv,"Special Defense");
        maxHp = DetermineHealthIncrease();
        if (currentLevel == 1)
            hp = maxHp;
    }
    float GetNatureModifier(string stat)
     {
         if (nature.statIncrease == stat)
             return 1.1f;
         if (nature.statDecrease == stat)
             return 0.9f;
         return 1;
     }
    float DetermineStatIncrease(float baseStat,float IV,float EV,string stat)
    {
        float brackets1 = (2*baseStat) + IV + (EV / 4);
        float bracket2 = brackets1 * (currentLevel / 100f);
        float bracket3 = bracket2 + 5f;
        return math.floor(bracket3 * GetNatureModifier(stat));
    }
    float DetermineHealthIncrease()
    {
        float brackets1 = (2*baseHp) + hpIv + (hpEv / 4);
        float bracket2 = brackets1 * (currentLevel / 100f);
        float bracket3 = bracket2 + currentLevel + 10f;
        return math.floor(bracket3);
    }
    public void LevelUp()
    {
        OnLevelUp?.Invoke(this);
        DetermineFriendshipLevelChange(true, "Level Up");
        currentLevel++;
        nextLevelExpAmount = PokemonOperations.CalculateExpForNextLevel(currentLevel,expGroup);
        IncreaseStats();
        if (!requiresEvolutionStone)
        {
            if(splitEvolution)
                DetermineSplitEvolution();
            else
                CheckEvolutionRequirements(0);
        }
        if (!Options_manager.Instance.playerInBattle)//artificial level up
            PokemonOperations.CheckForNewMove(this);
        OnNewLevel?.Invoke();
        while(currentExpAmount>nextLevelExpAmount)
            LevelUp();
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;
        OnDamageTaken?.Invoke();
    }
    private void ClearEvents()
    {
        OnDamageTaken = null;
        OnLevelUp = null;
        OnNewLevel = null;
    }
}
