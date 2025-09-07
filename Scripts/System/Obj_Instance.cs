using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public static class Obj_Instance
{
    public static Move CreateMove(Move m)
    {
        var newMove = ScriptableObject.CreateInstance<Move>();
        newMove.statusEffect = m.statusEffect;
        newMove.moveName = m.moveName;
        newMove.moveDamage = m.moveDamage;
        newMove.moveAccuracy = m.moveAccuracy;
        newMove.critModifierIndex = m.critModifierIndex;
        newMove.type = m.type;
        newMove.hasTypelessEffect = m.hasTypelessEffect;
        newMove.isSpecial = m.isSpecial;
        newMove.hasStatus = m.hasStatus;
        newMove.isContact = m.isContact;
        newMove.hasSpecialEffect = m.hasSpecialEffect;
        newMove.canCauseFlinch = m.canCauseFlinch;
        newMove.canCauseConfusion = m.canCauseConfusion;
        newMove.canTrap = m.canTrap;
        newMove.canInfatuate = m.canInfatuate;
        newMove.isBuffOrDebuff = m.isBuffOrDebuff;
        newMove.isConsecutive = m.isConsecutive;
        newMove.isSelfTargeted = m.isSelfTargeted;
        newMove.isMultiTarget = m.isMultiTarget;
        newMove.isSureHit = m.isSureHit;
        newMove.displayTargetMessage = m.displayTargetMessage;
        newMove.priority = m.priority;
        newMove.powerpoints = m.powerpoints;
        newMove.basePowerpoints = m.basePowerpoints;
        newMove.maxPowerpoints = m.maxPowerpoints;
        newMove.buffOrDebuffData = m.buffOrDebuffData;
        newMove.statusChance = m.statusChance;
        newMove.buffOrDebuffChance = m.buffOrDebuffChance;
        newMove.description = m.description;
        return newMove;
    }
    public static Pokemon CreatePokemon(Pokemon pkm)
    {
        Pokemon newPokemon = ScriptableObject.CreateInstance<Pokemon>();
        newPokemon.basePokemonName = pkm.basePokemonName;;
	    newPokemon.pokemonName = pkm.pokemonName;
        newPokemon.pokemonID  = pkm.pokemonID ;
        newPokemon.personalityValue = pkm.personalityValue;
        newPokemon.gender = pkm.gender;
        newPokemon.ratioFemale = pkm.ratioFemale;
        newPokemon.nature = pkm.nature;
        newPokemon.hasGender = pkm.hasGender;
        newPokemon.hp = pkm.hp;
        newPokemon.maxHp = pkm.maxHp;
        newPokemon.baseHp = pkm.baseHp;
        newPokemon.baseAttack = pkm.baseAttack;
        newPokemon.baseDefense = pkm.baseDefense;
        newPokemon.baseSpecialAttack = pkm.baseSpecialAttack;
        newPokemon.baseSpecialDefense = pkm.baseSpecialDefense;
        newPokemon.baseSpeed = pkm.baseSpeed;
        newPokemon.attack = pkm.attack;
        newPokemon.defense = pkm.defense;
        newPokemon.specialAttack = pkm.specialAttack;
        newPokemon.specialDefense = pkm.specialDefense;
        newPokemon.speed = pkm.speed;
        newPokemon.hpIv = pkm.hpIv;
        newPokemon.attackIv = pkm.attackIv;
        newPokemon.defenseIv = pkm.defenseIv;
        newPokemon.specialAttackIv = pkm.specialAttackIv;
        newPokemon.specialDefenseIv = pkm.specialDefenseIv;
        newPokemon.speedIv = pkm.speedIv;
        newPokemon.hpEv = pkm.hpEv;
        newPokemon.attackEv = pkm.attackEv;
        newPokemon.defenseEv = pkm.defenseEv;
        newPokemon.specialAttackEv = pkm.specialAttackEv;
        newPokemon.specialDefenseEv = pkm.specialDefenseEv;
        newPokemon.speedEv = pkm.speedEv;
        newPokemon.effortValues = pkm.effortValues;
        newPokemon.accuracy = pkm.accuracy;
        newPokemon.evasion = pkm.evasion;
        newPokemon.critChance = pkm.critChance;
        newPokemon.catchRate = pkm.catchRate;
        newPokemon.currentLevel = pkm.currentLevel;
        newPokemon.currentLevelExpAmount = pkm.currentLevelExpAmount;
        newPokemon.currentExpAmount = pkm.currentExpAmount;
        newPokemon.nextLevelExpAmount = pkm.nextLevelExpAmount;
        newPokemon.expGroup = pkm.expGroup;
        newPokemon.expYield = pkm.expYield;
        newPokemon.friendshipLevel = pkm.friendshipLevel;
        newPokemon.hasTrainer = pkm.hasTrainer;
        newPokemon.canBeFlinched = pkm.canBeFlinched;
        newPokemon.canBeInfatuated = pkm.canBeInfatuated;
        newPokemon.types = pkm.types;
        newPokemon.statusEffect = pkm.statusEffect;
        newPokemon.buffAndDebuffs = pkm.buffAndDebuffs;
        newPokemon.evolutionLineLevels = pkm.evolutionLineLevels;
        newPokemon.friendshipEvolutionRequirement = pkm.friendshipEvolutionRequirement;
        newPokemon.requiresEvolutionStone = pkm.requiresEvolutionStone;
        newPokemon.evolutionStone = pkm.evolutionStone;
        newPokemon.abilities = pkm.abilities;
        newPokemon.splitEvolution = pkm.splitEvolution;
        newPokemon.requiresFriendshipEvolution = pkm.requiresFriendshipEvolution;
        newPokemon.learnSet = pkm.learnSet;
        newPokemon.learnableTms = pkm.learnableTms;
        newPokemon.learnableHms = pkm.learnableHms;
        foreach (var move in pkm.moveSet)
            newPokemon.moveSet.Add(CreateMove(move));
        newPokemon.ability = pkm.ability;
        newPokemon.evolutions = pkm.evolutions;
        newPokemon.heldItem = pkm.heldItem;
        newPokemon.hasItem = pkm.hasItem;
        newPokemon.frontPicture = pkm.frontPicture;
        newPokemon.pokemonName = pkm.pokemonName;
        newPokemon.backPicture = pkm.backPicture;
        newPokemon.pokeballName = pkm.pokeballName;
        newPokemon.healthPhase = pkm.healthPhase;
        return newPokemon;
    }

    public static TrainerData CreateTrainer(TrainerData data)
    {
        var trainerCopy = ScriptableObject.CreateInstance<TrainerData>();
        trainerCopy.TrainerName = data.TrainerName;
        trainerCopy.TrainerType = data.TrainerType;        
        trainerCopy.BaseMoneyPayout = data.BaseMoneyPayout;
        trainerCopy.TrainerLocation = data.TrainerLocation;
        foreach (var member in data.PokemonParty)
            trainerCopy.PokemonParty.Add(CreateTrainerPokemonData(member));
        return trainerCopy;
    }

    private static TrainerPokemonData CreateTrainerPokemonData(TrainerPokemonData data)
    {
        var dataCopy = ScriptableObject.CreateInstance<TrainerPokemonData>();
        dataCopy.pokemon = CreatePokemon(data.pokemon);
        PokemonOperations.SetPokemonTraits(dataCopy.pokemon);
        dataCopy.moveSet = data.moveSet;
        dataCopy.pokemonLevel = data.pokemonLevel;
        dataCopy.hasItem = data.hasItem;
        dataCopy.heldItem = data.heldItem;
        return dataCopy;
    }
    public static Item CreateItem(Item item)
    {
        var newItem = ScriptableObject.CreateInstance<Item>();
        newItem.itemName = item.itemName;
        newItem.itemID = newItem.itemName + Utility.Random16Bit();
        newItem.itemType = item.itemType;
        newItem.itemDescription = item.itemDescription;
        newItem.price = item.price;
        newItem.itemImage = item.itemImage;
        newItem.quantity = item.quantity;
        newItem.itemEffect = item.itemEffect;
        newItem.forPartyUse = item.forPartyUse;
        newItem.canBeUsedInOverworld = item.canBeUsedInOverworld;
        newItem.canBeUsedInBattle = item.canBeUsedInBattle;
        newItem.isHeldItem = item.isHeldItem;
        newItem.canBeSold = item.canBeSold;
        newItem.canBeHeld = item.canBeHeld;
        newItem.hasModules = item.hasModules;
        newItem.isMultiModular = item.isMultiModular;
        newItem.additionalItemInfo = item.additionalItemInfo;
        newItem.additionalInfoModules = item.additionalInfoModules;
        newItem.infoModuleAssetNames = item.infoModuleAssetNames;
        newItem.imageDirectory = item.imageDirectory;
        return newItem;
    }
}
