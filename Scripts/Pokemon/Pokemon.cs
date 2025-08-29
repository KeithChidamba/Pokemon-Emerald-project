using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Pokemon", menuName = "pkm")]
public class Pokemon : ScriptableObject
{
    [FormerlySerializedAs("Base_Pokemon_name")] public string basePokemonName;
    [FormerlySerializedAs("Pokemon_name")] public string pokemonName;
    public long pokemonID = 0;
    public uint personalityValue;
    public PokemonOperations.Gender gender;
    public float ratioFemale = 0;
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
    public float hpEv=0;
    public float attackEv=0;
    public float defenseEv=0;
    public float specialAttackEv=0;
    public float specialDefenseEv=0;
    public float speedEv=0;
    [FormerlySerializedAs("EVs")] public List<EvYield> effortValues=new();
    public float accuracy = 100;
    public float evasion = 100;
    public float critChance = 6.25f;
    [FormerlySerializedAs("CatchRate")] public float catchRate = 0;
    public int currentLevel = 1;
    public int currentExpAmount = 0;
    public int currentLevelExpAmount;
    public int nextLevelExpAmount = 0;
    [FormerlySerializedAs("EXPGroup")] public PokemonOperations.ExpGroup expGroup;
    [FormerlySerializedAs("exp_yield")] public int expYield=0;
    public int friendshipLevel;
    public bool hasTrainer=false;
    public bool canBeFlinched = true;
    public bool canBeInfatuated = true;
    public List<Type> types;
    public PokemonOperations.StatusEffect statusEffect;
    public List<Buff_Debuff> buffAndDebuffs = new();
    [FormerlySerializedAs("evo_line")] public int[] evolutionLineLevels;
    public FriendShipEvolutionData friendshipEvolutionRequirement;
    [FormerlySerializedAs("RequiresEvolutionStone")] public bool requiresEvolutionStone = false;
    [FormerlySerializedAs("EvolutionStoneName")] public NameDB.EvolutionStone evolutionStone;
    public NameDB.Ability[] abilities;
    [FormerlySerializedAs("split_evolution")] public bool splitEvolution = false;
    public bool requiresFriendshipEvolution = false;
    public LearnSetMove[] learnSet;
    public List<NameDB.TM> learnableTms;
    public List<NameDB.HM> learnableHms;
    public List<Move> moveSet=new();
    public Ability ability;
    public List<Evolution> evolutions;
    public Item heldItem;
    public bool hasItem = false;
    [FormerlySerializedAs("front_picture")] public Sprite frontPicture;
    [FormerlySerializedAs("back_picture")] public Sprite backPicture;
    public string pokeballName;
    public int healthPhase;
    public event Action OnNewLevel;
    public event Action<Pokemon> OnLevelUp;
    public event Action<Pokemon> OnExpGainComplete;
    public event Action OnHealthChanged;
    //data conversion when json to obj
    public string abilityName;
    public string natureName;
    public List<string> evolutionNames=new();
    public List<string> typeNames = new();
    public List<MoveSaveData> moveData=new();
    
    public void SaveUnserializableData()
    {
        abilityName = ability.abilityName;
        natureName = nature.natureName;
        hasItem = (heldItem != null);
        moveData.Clear();
        typeNames.Clear();
        evolutionNames.Clear();

        foreach (var move in moveSet)
        {
            moveData.Add(new MoveSaveData(move.moveName.ToLower(), move.type.typeName.ToLower()
                , move.powerpoints, move.maxPowerpoints));
        }
        foreach (var type in types) typeNames.Add(type.typeName.ToLower());
        
        foreach (var evo in evolutions) evolutionNames.Add(evo.evolutionName);
    }
    public void LoadUnserializedData()//gives values to attributes that cant be deserialized, using saved values
    {
        frontPicture = Testing.CheckImage( Save_manager.GetDirectory
            (Save_manager.AssetDirectory.PokemonImage),pokemonName);
        backPicture =Testing.CheckImage( Save_manager.GetDirectory
            (Save_manager.AssetDirectory.PokemonImage),pokemonName+"_b");
        nature = Resources.Load<Nature>(Save_manager.GetDirectory
            (Save_manager.AssetDirectory.Natures) + natureName.ToLower());
        ability = Resources.Load<Ability>(Save_manager.GetDirectory
            (Save_manager.AssetDirectory.Abilities) + abilityName.ToLower());
        moveSet.Clear();
        types.Clear();
        evolutions.Clear();
        for (int i = 0; i < moveData.Count; i++)
        {
            var moveCopy = Obj_Instance.CreateMove(Resources.Load<Move>(
                Save_manager.GetDirectory(Save_manager.AssetDirectory.Moves) + moveData[i].moveName));
            
            moveCopy.powerpoints = moveData[i].powerPoints;
            moveCopy.maxPowerpoints = moveData[i].maxPowerPoints;
            moveSet.Add(moveCopy);
        }
        foreach (var typeName in typeNames)
        {
            types.Add(Resources.Load<Type>(Save_manager.GetDirectory
                (Save_manager.AssetDirectory.Types) + typeName));
        }
        foreach (var evolutionName in evolutionNames)
        {
            var pokemonDirectory = Save_manager.GetDirectory
                (Save_manager.AssetDirectory.Pokemon);
            evolutions.Add(Resources.Load<Evolution>($"{pokemonDirectory}{basePokemonName}/{evolutionName}"));
        }
        for (int i = 0; i < types.Count; i++)
        {
            types[i].typeImage = Resources.Load<Sprite>(Save_manager.GetDirectory
                (Save_manager.AssetDirectory.UI) + typeNames[i]);
        }
        Battle_handler.Instance.OnBattleEnd += ClearEvents;
    }

    public void RemoveHeldItem()
    {
        heldItem = null;
        hasItem = false;
    }

    public void GiveItem(Item itemToGive)
    {
        hasItem = true;
        heldItem = Obj_Instance.CreateItem(itemToGive);
        heldItem.quantity = 1;
    }
    public bool HasType(PokemonOperations.Types typeName)
    {
        return types.Any(type=>type.typeName==typeName.ToString());
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
    public void DetermineFriendshipLevelChange(bool isIncreasing, PokemonOperations.FriendshipModifier action)
    {
        if (friendshipLevel > 254 && isIncreasing) return;
        
        var isHighFriendship = friendshipLevel > 219;

        if (action == PokemonOperations.FriendshipModifier.Fainted)
        {
            ChangeFriendshipLevel( friendshipLevel>99? -1 : -5 );
        }
        
        if (action == PokemonOperations.FriendshipModifier.LevelUp)
        {
            var increaseLimitLow = (isHighFriendship)? 1 : 3;
            var increaseLimitHigh = (isHighFriendship)? 3 : 6;
            var randomIncrease = Utility.RandomRange(increaseLimitLow, increaseLimitHigh);
            var amount = ApplyFriendshipModifier(randomIncrease);
            ChangeFriendshipLevel( isIncreasing?  amount : -amount );
        }
        
        if (action == PokemonOperations.FriendshipModifier.Vitamin)
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

        if (action == PokemonOperations.FriendshipModifier.Berry)
        {
            ChangeFriendshipLevel(ApplyFriendshipModifier(10) );
        }
    }
    public void CheckEvolutionRequirements(int evoIndex)
    {
        if (requiresEvolutionStone)
        {
            Evolve(evolutions[evoIndex]); return;
        }

        if (requiresFriendshipEvolution)
        {
            if (friendshipLevel >= friendshipEvolutionRequirement.friendshipRequirement)
            {
                Evolve(evolutions[friendshipEvolutionRequirement.evolutionIndex]);
            } 
            return;
        }
        
        //regular evolution
        for (int i = 0; i < evolutionLineLevels.Length; i++)
        {
            var requiredLevelToEvolve = evolutionLineLevels[i];
            if (currentLevel == requiredLevelToEvolve)
            {
                Evolve(evolutions[i+evoIndex]);
                break;
            }
        }
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
    public IEnumerator ReceiveExperienceAndDisplay(int amount)
    {
        if (currentLevel >= 100) yield break;
        
        int remainingExp = amount;
        Battle_handler.Instance.StartExpEvent(this);
        
        while (remainingExp > 0 && currentLevel < 100)
        {
            int expToNextLevel = nextLevelExpAmount - currentExpAmount;
            
            // How much EXP should we add this loop? Either all remaining or just enough to hit the next level.
            int expThisLoop = Mathf.Min(remainingExp, expToNextLevel);
            float displayExp = currentExpAmount;
            var expAfterChange = currentExpAmount + expThisLoop;
            
            // Animate EXP bar filling
            var increaseDeltaMultiplier = expThisLoop / 100f > 3? expThisLoop / 100f: 3;
            while (displayExp < expAfterChange)
            {
                displayExp = Mathf.MoveTowards(displayExp,expAfterChange , 
                    30f * increaseDeltaMultiplier * Time.deltaTime);
                currentExpAmount = (int)Mathf.Floor(displayExp);
                yield return null;
            }

            // Deduct what we just gave
            remainingExp -= expThisLoop;

            // If we reached or passed the next level threshold > level up
            if (currentExpAmount >= nextLevelExpAmount && currentLevel < 100)
            {
                LevelUp();
                Dialogue_handler.Instance.DisplayBattleInfo("Wow!");
                Dialogue_handler.Instance.DisplayBattleInfo(pokemonName+" leveled up!");
                currentLevelExpAmount = currentExpAmount;
                yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
                PokemonOperations.CheckForNewMove(this);
            
                if (PokemonOperations.LearningNewMove)
                {
                    if (moveSet.Count == 4)
                    {
                        yield return new WaitUntil(() => PokemonOperations.SelectingMoveReplacement);
                        yield return new WaitUntil(() => !PokemonOperations.SelectingMoveReplacement);
                    }
                    yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
                }
                yield return new WaitUntil(() => !PokemonOperations.LearningNewMove);
                
                nextLevelExpAmount = PokemonOperations.CalculateExpForNextLevel(currentLevel, expGroup);
            }
        }
        
        OnExpGainComplete?.Invoke(this);
    }

    public int CalculateExperience(Pokemon enemy)
    {
        var trainerBonus = 1f;
        var baseExp = (enemy.expYield*enemy.currentLevel) / 7f;
        var expItemBonus = 1f;
        if (hasItem)
            if (heldItem.itemType == Item_handler.ItemType.GainExp)//lucky egg
                expItemBonus = float.Parse(heldItem.itemEffect);
        if (enemy.hasTrainer)
            trainerBonus = 1.5f;
        return (int)math.trunc(baseExp * trainerBonus * expItemBonus);
    }
    public void Evolve(Evolution evo)
    {
        pokemonName = evo.evolutionName;
        effortValues=evo.effortValues;
        types = evo.types;
        ability = evo.ability;
        learnableTms = new List<NameDB.TM>(evo.learnableTms);
        learnableHms = new List<NameDB.HM>(evo.learnableHms);
        learnSet = (LearnSetMove[])evo.learnSet.Clone();
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
        if (Pokemon_party.Instance.party.Contains(this))
            Pokemon_party.Instance.RefreshMemberCards();
    }

    void IncreaseStats()
    {
        attack = DetermineStatIncrease(baseAttack,attackIv,attackEv,PokemonOperations.Stat.Attack);
        defense = DetermineStatIncrease(baseDefense,defenseIv,defenseEv,PokemonOperations.Stat.Defense);
        speed = DetermineStatIncrease(baseSpeed,speedIv,speedEv,PokemonOperations.Stat.Speed);
        specialAttack = DetermineStatIncrease(baseSpecialAttack,specialAttackIv,specialAttackEv,PokemonOperations.Stat.SpecialAttack);
        specialDefense = DetermineStatIncrease(baseSpecialDefense,specialDefenseIv,specialDefenseEv,PokemonOperations.Stat.SpecialDefense);
        maxHp = DetermineHealthIncrease();
        if (currentLevel == 1)
            hp = maxHp;
    }
    float GetNatureModifier(PokemonOperations.Stat stat)
     {
         if (nature.statToIncrease == stat)
             return 1.1f;
         if (nature.statToDecrease == stat)
             return 0.9f;
         return 1;
     }
    float DetermineStatIncrease(float baseStat,float IV,float EV,PokemonOperations.Stat stat)
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
        DetermineFriendshipLevelChange(true, PokemonOperations.FriendshipModifier.LevelUp);
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

    public void ChangeHealth()
    {
        OnHealthChanged?.Invoke();
    }
    private void ClearEvents()
    {
        OnLevelUp = null;
        OnNewLevel = null;
    }
}
