using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
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
    [FormerlySerializedAs("Pokemon_name")] public string pokemonName;
    public string nickName;
    public string currentPokemonName;
    public CaptureInformation captureInformation;
    public long pokemonID;
    public uint personalityValue;
    public bool isShiny;
    public Gender gender;
    public float ratioFemale;
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
    public float hpEv;
    public float attackEv;
    public float defenseEv;
    public float specialAttackEv;
    public float specialDefenseEv;
    public float speedEv;
    [FormerlySerializedAs("EVs")] public List<EvYield> effortValues=new();
    public float accuracy = 100;
    public float evasion = 100;
    public float critChance = 6.25f;
    [FormerlySerializedAs("CatchRate")] public float catchRate = 0;
    public int currentLevel = 1;
    public int currentExpAmount;
    public int currentLevelExpAmount;
    public int nextLevelExpAmount;
    [FormerlySerializedAs("EXPGroup")] public ExpGroup expGroup;
    [FormerlySerializedAs("exp_yield")] public int expYield=0;
    public bool canEvolve = true;
    public int friendshipLevel;
    public bool hasTrainer;
    public bool canBeFlinched = true;
    public bool canBeInfatuated = true;
    public List<Type> types;
    public StatusEffect statusEffect;
    public List<Buff_Debuff> buffAndDebuffs = new();
    [FormerlySerializedAs("evo_line")] public int[] evolutionLineLevels;
    public int currentEvolutionLineIndex;
    public FriendShipEvolutionData friendshipEvolutionRequirement;
    [FormerlySerializedAs("RequiresEvolutionStone")] public bool requiresEvolutionStone = false;
    [FormerlySerializedAs("EvolutionStoneName")] public EvolutionStone evolutionStone;
    public AbilityName[] abilities;
    [FormerlySerializedAs("split_evolution")] public bool splitEvolution = false;
    public bool requiresFriendshipEvolution = false;
    public LearnSetMove[] learnSet;
    public List<TM_Name> learnableTms;
    public List<HM_Name> learnableHms;
    public List<Move> moveSet=new();
    public Ability ability;
    public List<Evolution> evolutions;
    public Item heldItem;
    public bool hasItem = false;
    [FormerlySerializedAs("front_picture")] public Sprite frontPicture;
    [FormerlySerializedAs("back_picture")] public Sprite backPicture;
    public Sprite partyFrame1;
    public Sprite partyFrame2;
    public Sprite battleIntroFrame;
    public string pokeballName;
    public int healthPhase;
    public event Action OnNewLevel;
    public event Action<Pokemon> OnLevelUp;
    public event Action<Pokemon> OnExpGainComplete;
    public event Action<Battle_Participant> OnHealthChanged;
    public event Action<int> OnEvolutionSuccessful;
    //data conversion when json to obj
    public string abilityName;
    public string natureName;
    public List<string> evolutionNames=new();
    public List<string> typeNames = new();
    public List<MoveSaveData> moveData=new();
    
    //dependencies
    private Dialogue_handler _dialogueHandler;
    private Battle_handler _battleHandler;
    private Pokemon_party _pokemonPartyHandler;
    private PokemonOperations _pokemonOperationsHandler;
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
            moveData.Add(new MoveSaveData(move.moveName.ToLower(), move.powerpoints, move.maxPowerpoints));
        }
        foreach (var type in types) typeNames.Add(type.GetTypeName.ToLower());
        
        foreach (var evo in evolutions) evolutionNames.Add(evo.evolutionName);
    }
    public void LoadDataAndDependencies(ServiceContainer serviceContainer)
    {//gives values to attributes that cant be deserialized, using saved values
        frontPicture = Testing.GetValidImage( SaveDataHandler.GetDirectory
            (AssetDirectory.PokemonImage),pokemonName + (isShiny?"_s":"_f"));
        backPicture =Testing.GetValidImage( SaveDataHandler.GetDirectory
            (AssetDirectory.PokemonImage),pokemonName+ (isShiny?"_sb":"_b"));
        partyFrame1=Testing.GetValidImage( SaveDataHandler.GetDirectory
            (AssetDirectory.PokemonPartyImage),pokemonName+"_1");
        partyFrame2=Testing.GetValidImage( SaveDataHandler.GetDirectory
            (AssetDirectory.PokemonPartyImage),pokemonName+"_2");
        battleIntroFrame=Testing.GetValidImage( SaveDataHandler.GetDirectory
            (AssetDirectory.PokemonImage),pokemonName+ (isShiny?"_s_intro":"_intro"));
        
        nature = Resources.Load<Nature>(SaveDataHandler.GetDirectory
            (AssetDirectory.Natures) + natureName.ToLower());
        ability = Resources.Load<Ability>(SaveDataHandler.GetDirectory
            (AssetDirectory.Abilities) + abilityName.ToLower());
        types.Clear();
        evolutions.Clear();
        
        LoadMovesFromSource(moveData);
        
        foreach (var typeName in typeNames)
        {
            types.Add(Resources.Load<Type>(SaveDataHandler.GetDirectory
                (AssetDirectory.Types) + typeName));
        }
        foreach (var evolutionName in evolutionNames)
        {
            var pokemonDirectory = SaveDataHandler.GetDirectory
                (AssetDirectory.Pokemon);
            evolutions.Add(Resources.Load<Evolution>($"{pokemonDirectory}{basePokemonName}/{evolutionName}"));
        }
        for (int i = 0; i < types.Count; i++)
        {
            types[i].typeImage = Resources.Load<Sprite>(SaveDataHandler.GetDirectory
                (AssetDirectory.UI) + typeNames[i]);
        }

        Inject(serviceContainer);

        _battleHandler.OnBattleEnd += ClearEvents;
    }

    public void Inject(ServiceContainer serviceContainer)
    {
        _dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        _battleHandler = serviceContainer.Resolve<Battle_handler>(); 
        _pokemonPartyHandler = serviceContainer.Resolve<Pokemon_party>();
        _pokemonOperationsHandler = serviceContainer.Resolve<PokemonOperations>();
    }
    public void ResetMoveData()
    {
        List<MoveSaveData> uniqueMoveData = new ();
        moveSet.ForEach(move=>uniqueMoveData.Add(
            new (move.moveName,move.powerpoints,move.maxPowerpoints)
            ));
        LoadMovesFromSource(uniqueMoveData);
    }
    private void LoadMovesFromSource(List<MoveSaveData> movesetTemplate)
    {
        moveSet.Clear();
        foreach (var move in movesetTemplate)
        {
            var moveCopy = InstanceFactory.CreateMove(Resources.Load<Move>(
                SaveDataHandler.GetDirectory(AssetDirectory.Moves) + move.moveName));
            
            moveCopy.powerpoints = move.powerPoints;
            moveCopy.maxPowerpoints = move.maxPowerpoints;
            moveSet.Add(moveCopy);
        }
    }
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
        float modifier = 1f;
        if (pokeballName == "Luxury Ball")
            modifier *= 1.5f;
        
        if (hasItem)
            if(heldItem.itemName == "Soothe Bell")
                modifier *= 1.5f;
        return (int)math.ceil(currentIncrease * modifier);
    }

    public void ChangeFriendshipLevel(int amount)
    {
        friendshipLevel = Math.Clamp(friendshipLevel + amount, 0, 255);
    }
    public void DetermineFriendshipLevelChange(bool isIncreasing, FriendshipModifier action)
    {
        if (friendshipLevel > 254 && isIncreasing) return;
        
        var isHighFriendship = friendshipLevel > 219;

        if (action == FriendshipModifier.Fainted)
        {
            ChangeFriendshipLevel( friendshipLevel>99? -1 : -5 );
        }
        
        if (action == FriendshipModifier.LevelUp)
        {
            var increaseLimitLow = (isHighFriendship)? 1 : 3;
            var increaseLimitHigh = (isHighFriendship)? 3 : 6;
            var randomIncrease = Utility.RandomRange(increaseLimitLow, increaseLimitHigh);
            var amount = ApplyFriendshipModifier(randomIncrease);
            ChangeFriendshipLevel( isIncreasing?  amount : -amount );
        }
        
        if (action == FriendshipModifier.Vitamin)
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

        if (action == FriendshipModifier.Berry)
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

        var isPlayerPokemon = _pokemonPartyHandler.party.Contains(this);
        
        if (requiresFriendshipEvolution)
        {
            if (friendshipLevel >= friendshipEvolutionRequirement.friendshipRequirement)
            {
                if (isPlayerPokemon && _battleHandler.battleInProgress)
                {
                    OnEvolutionSuccessful?.Invoke(friendshipEvolutionRequirement.evolutionIndex);
                }
                else
                {
                    Evolve(evolutions[friendshipEvolutionRequirement.evolutionIndex]);
                }
            } 
            return;
        } 
        
//regular evolution
        if (currentEvolutionLineIndex >= evolutionLineLevels.Length)
        {
            return;//max evolution
        }
        
        if (currentLevel>=evolutionLineLevels[currentEvolutionLineIndex])
        {
            if (isPlayerPokemon && _battleHandler.battleInProgress)
            {
                OnEvolutionSuccessful?.Invoke(currentEvolutionLineIndex+evoIndex);
            }
            else
            {
                Evolve(evolutions[currentEvolutionLineIndex+evoIndex]);
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
        _battleHandler.StartExpEvent(this);
        
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

                _dialogueHandler.DisplayBattleInfo(pokemonName+" grew to lv"+currentLevel);
               
                yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
                yield return _pokemonOperationsHandler.WaitForNewMoveCheck(this);

                yield return _pokemonOperationsHandler.AwaitMoveOperation(moveSet.Count == 4);
                
                nextLevelExpAmount = PokemonOperations.CalculateExpForNextLevel(currentLevel, expGroup);
            }
        }
        _dialogueHandler.DisplayBattleInfo($"{pokemonName} gained {amount} EXP points");
        OnExpGainComplete?.Invoke(this);
    }

    public int CalculateExperience()
    {
        var trainerBonus = 1f;
        var baseExp = (expYield*currentLevel) / 7f;
        var expItemBonus = 1f;
        if (hasItem)
        {
            if (heldItem.itemType == ItemType.GainExp)
            {
                expItemBonus = heldItem.itemEffectData;
            }
        }
        
        if (hasTrainer) trainerBonus = 1.5f;
        
        return (int)math.trunc(baseExp * trainerBonus * expItemBonus);
    }
    public void Evolve(Evolution evo)
    {
        pokemonName = evo.evolutionName;
        effortValues=evo.effortValues;
        types = evo.types;
        ability = evo.ability;
        learnableTms = new List<TM_Name>(evo.learnableTms);
        learnableHms = new List<HM_Name>(evo.learnableHms);
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
        if (_pokemonPartyHandler.party.Contains(this))
            _pokemonPartyHandler.RefreshMemberCards();
    }

    void IncreaseStats()
    {
        attack = DetermineStatIncrease(baseAttack,attackIv,attackEv,Stat.Attack);
        defense = DetermineStatIncrease(baseDefense,defenseIv,defenseEv,Stat.Defense);
        speed = DetermineStatIncrease(baseSpeed,speedIv,speedEv,Stat.Speed);
        specialAttack = DetermineStatIncrease(baseSpecialAttack,specialAttackIv,specialAttackEv,Stat.SpecialAttack);
        specialDefense = DetermineStatIncrease(baseSpecialDefense,specialDefenseIv,specialDefenseEv,Stat.SpecialDefense);
        maxHp = DetermineHealthIncrease();
        if (currentLevel == 1)
            hp = maxHp;
    }
    float GetNatureModifier(Stat stat)
     {
         if (nature.statToIncrease == stat)
             return 1.1f;
         if (nature.statToDecrease == stat)
             return 0.9f;
         return 1;
     }
    float DetermineStatIncrease(float baseStat,float IV,float EV,Stat stat)
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
    private void LevelUp()
    {
        OnLevelUp?.Invoke(this);
        DetermineFriendshipLevelChange(true, FriendshipModifier.LevelUp);
        currentLevel++;
        currentLevelExpAmount = PokemonOperations.CalculateExpForLevel(currentLevel,expGroup);
        nextLevelExpAmount = PokemonOperations.CalculateExpForNextLevel(currentLevel,expGroup);
        IncreaseStats();
        
        if (!requiresEvolutionStone)
        {
            if (canEvolve)
            {
                if(splitEvolution)
                    DetermineSplitEvolution();
                else
                    CheckEvolutionRequirements(0);
            }
        }
        if (!_battleHandler.battleInProgress)//artificial level up
            _pokemonOperationsHandler.CheckForNewMove(this);
        OnNewLevel?.Invoke();
        while(currentExpAmount>nextLevelExpAmount)
            LevelUp();
    }

    public void ChangeHealth(Battle_Participant attacker)
    {
        OnHealthChanged?.Invoke(attacker);
    }
    private void ClearEvents()
    {
        OnLevelUp = null;
        OnNewLevel = null;
        OnEvolutionSuccessful = null;
    }
}
