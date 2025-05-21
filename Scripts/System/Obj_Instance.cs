using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public static class Obj_Instance
{
    public static Move CreateMove(Move m)
    {
        var newMove = ScriptableObject.CreateInstance<Move>();
        newMove.Status_effect = m.Status_effect;
        newMove.Move_name = m.Move_name;
        newMove.Move_damage = m.Move_damage;
        newMove.Move_accuracy = m.Move_accuracy;
        newMove.type = m.type;
        newMove.isSpecial = m.isSpecial;
        newMove.Has_status = m.Has_status;
        newMove.Has_effect = m.Has_effect;
        newMove.Can_flinch = m.Can_flinch;
        newMove.is_Buff_Debuff = m.is_Buff_Debuff;
        newMove.is_Consecutive = m.is_Consecutive;
        newMove.isSelfTargeted = m.isSelfTargeted;
        newMove.isMultiTarget = m.isMultiTarget;
        newMove.Priority = m.Priority;
        newMove.Powerpoints = m.Powerpoints;
        newMove.BasePowerpoints = m.BasePowerpoints;
        newMove.max_Powerpoints = m.max_Powerpoints;
        newMove.Buff_Debuff = m.Buff_Debuff;
        newMove.Status_chance = m.Status_chance;
        newMove.Debuff_chance = m.Debuff_chance;
        newMove.Description = m.Description;
        return newMove;
    }
    public static Pokemon CreatePokemon(Pokemon pkm)
    {
        Pokemon newPokemon = ScriptableObject.CreateInstance<Pokemon>();
        newPokemon.Base_Pokemon_name = pkm.Base_Pokemon_name;;
	    newPokemon.Pokemon_name = pkm.Pokemon_name;
        newPokemon.Pokemon_ID  = pkm.Pokemon_ID ;
        newPokemon.Personality_value = pkm.Personality_value;
        newPokemon.Gender = pkm.Gender;
        newPokemon.GenderRatio = pkm.GenderRatio;
        newPokemon.nature = pkm.nature;
        newPokemon.has_gender = pkm.has_gender;
        newPokemon.HP = pkm.HP;
        newPokemon.max_HP = pkm.max_HP;
        newPokemon.BaseHP = pkm.BaseHP;
        newPokemon.BaseAttack = pkm.BaseAttack;
        newPokemon.BaseDefense = pkm.BaseDefense;
        newPokemon.BaseSP_ATK = pkm.BaseSP_ATK;
        newPokemon.BaseSP_DEF = pkm.BaseSP_DEF;
        newPokemon.Basespeed = pkm.Basespeed;
        newPokemon.Attack = pkm.Attack;
        newPokemon.Defense = pkm.Defense;
        newPokemon.SP_ATK = pkm.SP_ATK;
        newPokemon.SP_DEF = pkm.SP_DEF;
        newPokemon.speed = pkm.speed;
        newPokemon.HP_IV = pkm.HP_IV;
        newPokemon.Attack_IV = pkm.Attack_IV;
        newPokemon.Defense_IV = pkm.Defense_IV;
        newPokemon.SP_ATK_IV = pkm.SP_ATK_IV;
        newPokemon.SP_DEF_IV = pkm.SP_DEF_IV;
        newPokemon.speed_IV = pkm.speed_IV;
        newPokemon.HP_EV = pkm.HP_EV;
        newPokemon.Attack_EV = pkm.Attack_EV;
        newPokemon.Defense_EV = pkm.Defense_EV;
        newPokemon.SP_ATK_EV = pkm.SP_ATK_EV;
        newPokemon.SP_DEF_EV = pkm.SP_DEF_EV;
        newPokemon.speed_EV = pkm.speed_EV;
        newPokemon.EVs = pkm.EVs;
        newPokemon.Accuracy = pkm.Accuracy;
        newPokemon.Evasion = pkm.Evasion;
        newPokemon.crit_chance = pkm.crit_chance;
        newPokemon.CatchRate = pkm.CatchRate;
        newPokemon.Current_level = pkm.Current_level;
        newPokemon.CurrentExpAmount = pkm.CurrentExpAmount;
        newPokemon.NextLevelExpAmount = pkm.NextLevelExpAmount;
        newPokemon.EXPGroup = pkm.EXPGroup;
        newPokemon.exp_yield = pkm.exp_yield;
        newPokemon.has_trainer = pkm.has_trainer;
        newPokemon.canAttack = pkm.canAttack;
        newPokemon.isFlinched = pkm.isFlinched;
        newPokemon.CanBeDamaged = pkm.CanBeDamaged;
        newPokemon.immuneToStatReduction = pkm.immuneToStatReduction;
        newPokemon.types = pkm.types;
        newPokemon.Status_effect = pkm.Status_effect;
        newPokemon.Buff_Debuffs = pkm.Buff_Debuffs;
        newPokemon.evo_line = pkm.evo_line;
        newPokemon.abilities = pkm.abilities;
        newPokemon.split_evolution = pkm.split_evolution;
        newPokemon.learnSet = pkm.learnSet;
        foreach (var move in pkm.move_set)
            newPokemon.move_set.Add(CreateMove(move));
        newPokemon.ability = pkm.ability;
        newPokemon.evolutions = pkm.evolutions;
        newPokemon.HeldItem = pkm.HeldItem;
        newPokemon.HasItem = pkm.HasItem;
        newPokemon.front_picture = pkm.front_picture;
        newPokemon.Pokemon_name = pkm.Pokemon_name;
        newPokemon.back_picture = pkm.back_picture;
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
        return newItem;
    }
}
