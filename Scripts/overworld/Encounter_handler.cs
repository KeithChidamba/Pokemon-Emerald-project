using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Encounter_handler : MonoBehaviour
{
    public Encounter_Area currentArea;
    public bool encounterTriggered = false;
    public Pokemon wildPokemon;
    public int encounterChance = 2;
    public static Encounter_handler Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void TriggerEncounter(Encounter_Area area)
    {
        currentArea = area;
        encounterTriggered = true;
        encounterChance = 2;
        for (int i = 0; i < currentArea.availablePokemon.Length; i++)
        {
            if (EncounteredPokemon(i)) break;
        }
    }
    bool EncounteredPokemon(int currentIndex)
    {
        var random = Utility.RandomRange(1,101);
        var chance = int.Parse(currentArea.availablePokemon[currentIndex].Split('/')[1]);

        if ( currentIndex == currentArea.availablePokemon.Length - 1 /*pick last option if none in range*/ 
             || random < chance )//pick option within chance range
        {
            var pokemonName = currentArea.availablePokemon[currentIndex].Split('/')[0];
            CreateWildPokemon(pokemonName);
            return true;
        }
        return false;
    }
    public void TriggerFishingEncounter(Encounter_Area area,Item fishingRod)
    {
        currentArea = area;
        encounterTriggered = true;
        encounterChance = 2;
        area.minimumLevelOfPokemon = int.Parse(fishingRod.itemEffect.Split('/')[0]);
        area.maximumLevelOfPokemon = int.Parse(fishingRod.itemEffect.Split('/')[1]);
        //the type of rod determines available pokemon from pool
        var rodTypeIndex = fishingRod.itemName switch 
        {
            "Old Rod"=>0,
            "Good Rod"=>1,
            "Super Rod"=>2,
            _=>0
        } ;
        int availablePokemonForRod = area.pokemonIndexForRodType[rodTypeIndex];
        for (int i = 0; i < availablePokemonForRod+1; i++)
        {
            if (EncounteredPokemon(i)) break;
        }
    }
    void CreateWildPokemon(string pokemonName)
    {
        wildPokemon = Obj_Instance.CreatePokemon(Resources.Load<Pokemon>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + pokemonName.ToLower()+"/"+ pokemonName.ToLower()));
        if (wildPokemon != null)
        {
            PokemonOperations.SetPokemonTraits(wildPokemon);
            var randomLevel = Utility.RandomRange(currentArea.minimumLevelOfPokemon, currentArea.maximumLevelOfPokemon+1);
            var expForRequiredLevel = PokemonOperations.CalculateExpForNextLevel(randomLevel - 1, wildPokemon.expGroup)+1;
            wildPokemon.ReceiveExperience(expForRequiredLevel);
            wildPokemon.hp=wildPokemon.maxHp;
           Battle_handler.Instance.StartWildBattle(wildPokemon);
        }
        else
            Debug.Log("tried encounter but didnt find "+pokemonName);
    }
    void TurnOffTrigger()
    {
        encounterTriggered = false;
    }
    public void ResetTrigger()
    {
        Invoke(nameof(TurnOffTrigger),1f);
    }
}
