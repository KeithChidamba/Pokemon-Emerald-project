using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PokemonSaveData
{
    public string basePokemonName;
    public string pokemonName;
    public string nickName;
    public string pokemonDisplayName;
    
    public CaptureInformation captureInformation;
    public long pokemonID;
    public uint personalityValue;
    public bool isShiny;
    public bool hasGender = true;
    public Gender gender;
    public float hp;
    public float maxHp;
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
    public int currentLevel = 1;
    public int currentExpAmount;
    public int currentLevelExpAmount;
    public int nextLevelExpAmount;
    public int friendshipLevel;
    public bool hasTrainer;
    public StatusEffect statusEffect;
    public string pokeballName;
    public int currentEvolutionLineIndex;
    public bool hasItem;
   
   public string abilityName;
   public string natureName;
   public List<MoveSaveData> moveData=new();
   public string evolutionName;

   public void SetupSave(Pokemon saveSource)
   {
       abilityName = NameDB.GetAbility(saveSource.ability.abilityName);
       natureName = saveSource.nature.natureName;
       evolutionName = saveSource.evolutions[currentEvolutionLineIndex].evolutionName;
       hasItem = saveSource.heldItem is not null;
       moveData.Clear();
       foreach (var move in saveSource.moveSet)
       {
           moveData.Add(new MoveSaveData(move.moveName.ToLower(), move.powerpoints, move.maxPowerpoints));
       }
       basePokemonName = saveSource.basePokemonName;
       pokemonName = saveSource.pokemonName;
       nickName = saveSource.nickName;
       pokemonDisplayName = saveSource.pokemonDisplayName;

       captureInformation = saveSource.captureInformation;
       pokemonID = saveSource.pokemonID;
       personalityValue = saveSource.personalityValue;
       isShiny = saveSource.isShiny;
       hasGender = saveSource.hasGender;
       gender = saveSource.gender;

       hp = saveSource.hp;
       maxHp = saveSource.maxHp;
       attack = saveSource.attack;
       defense = saveSource.defense;
       specialAttack = saveSource.specialAttack;
       specialDefense = saveSource.specialDefense;
       speed = saveSource.speed;

       hpIv = saveSource.hpIv;
       attackIv = saveSource.attackIv;
       defenseIv = saveSource.defenseIv;
       specialAttackIv = saveSource.specialAttackIv;
       specialDefenseIv = saveSource.specialDefenseIv;
       speedIv = saveSource.speedIv;

       hpEv = saveSource.hpEv;
       attackEv = saveSource.attackEv;
       defenseEv = saveSource.defenseEv;
       specialAttackEv = saveSource.specialAttackEv;
       specialDefenseEv = saveSource.specialDefenseEv;
       speedEv = saveSource.speedEv;

       currentLevel = saveSource.currentLevel;
       currentExpAmount = saveSource.currentExpAmount;
       currentLevelExpAmount = saveSource.currentLevelExpAmount;
       nextLevelExpAmount = saveSource.nextLevelExpAmount;
       friendshipLevel = saveSource.friendshipLevel;
       hasTrainer = saveSource.hasTrainer;
       statusEffect = saveSource.statusEffect;

       pokeballName = saveSource.pokeballName;
       currentEvolutionLineIndex = saveSource.currentEvolutionLineIndex;
   }
   
   public void LoadUniqueData(Pokemon newPokemon)
   {
       var pokemonDirectory = DirectoryHandler.GetDirectory(AssetDirectory.Pokemon);
       if (currentEvolutionLineIndex > 0)
       {
           var evo = Resources.Load<Evolution>($"{pokemonDirectory}{basePokemonName}/{evolutionName}");
           newPokemon.types = evo.types;
           newPokemon.learnableTms = new List<MoveName>(evo.learnableTms);
           newPokemon.learnableHms = new List<MoveName>(evo.learnableHms);
           newPokemon.learnSet = (LearnSetMove[])evo.learnSet.Clone();
           newPokemon.frontPicture = evo.frontPicture;
           newPokemon.backPicture = evo.backPicture;
           newPokemon.partyFrame1 = evo.partyFrame1;
           newPokemon.partyFrame2 = evo.partyFrame2;
           newPokemon.battleIntroFrame = evo.battleIntroFrame;
           newPokemon.expGroup = evo.expGroup;
           newPokemon.expYield = evo.expYield;
           newPokemon.catchRate = evo.catchRate;
           newPokemon.baseHp=evo.baseHp;
           newPokemon.baseAttack=evo.baseAttack;
           newPokemon.baseDefense=evo.baseDefense;
           newPokemon.baseSpecialAttack=evo.baseSpecialAttack;
           newPokemon.baseSpecialDefense=evo.baseSpecialDefense;
           newPokemon.baseSpeed = evo.baseSpeed;
           newPokemon.requiresFriendshipEvolution = evo.requiresFriendshipEvolution;
           newPokemon.requiresEvolutionStone = evo.requiresEvolutionStone;
           newPokemon.friendshipEvolutionRequirement = evo.friendshipEvolutionRequirement;
       }
       
       newPokemon.currentEvolutionLineIndex = currentEvolutionLineIndex;
       newPokemon.pokeballName = pokeballName;
       newPokemon.hasItem = hasItem;
       newPokemon.basePokemonName = basePokemonName;
       newPokemon.pokemonName = pokemonName;
       newPokemon.nickName = nickName;
       newPokemon.pokemonDisplayName = pokemonDisplayName;
       newPokemon.captureInformation = captureInformation;
       newPokemon.pokemonID = pokemonID;
       newPokemon.personalityValue = personalityValue;
       newPokemon.isShiny = isShiny;
       newPokemon.hasGender = hasGender;
       newPokemon.gender = gender;
       newPokemon.hp = hp;
       newPokemon.maxHp = maxHp;
       newPokemon.attack = attack;
       newPokemon.defense = defense;
       newPokemon.specialAttack = specialAttack;
       newPokemon.specialDefense = specialDefense;
       newPokemon.speed = speed;
       newPokemon.hpIv = hpIv;
       newPokemon.attackIv = attackIv;
       newPokemon.defenseIv = defenseIv;
       newPokemon.specialAttackIv = specialAttackIv;
       newPokemon.specialDefenseIv = specialDefenseIv;
       newPokemon.speedIv = speedIv;
       newPokemon.hpEv = hpEv;
       newPokemon.attackEv = attackEv;
       newPokemon.defenseEv = defenseEv;
       newPokemon.specialAttackEv = specialAttackEv;
       newPokemon.specialDefenseEv = specialDefenseEv;
       newPokemon.speedEv = speedEv;
       newPokemon.currentLevel = currentLevel;
       newPokemon.currentExpAmount = currentExpAmount;
       newPokemon.currentLevelExpAmount = currentLevelExpAmount;
       newPokemon.nextLevelExpAmount = nextLevelExpAmount;
       newPokemon.friendshipLevel = friendshipLevel;
       newPokemon.hasTrainer = hasTrainer;
       newPokemon.statusEffect = statusEffect;
       
       newPokemon.nature = Resources.Load<Nature>(DirectoryHandler.GetDirectory
           (AssetDirectory.Natures) + natureName.ToLower());
       newPokemon.ability = Resources.Load<Ability>(DirectoryHandler.GetDirectory
           (AssetDirectory.Abilities) + abilityName.ToLower());
       
       PokemonOperations.LoadMovesFromSource(moveData,newPokemon);
   }
}
