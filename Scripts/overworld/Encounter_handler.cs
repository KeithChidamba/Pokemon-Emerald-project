using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public enum BattleSource
{
    None,Fishing,TallGrass
}
public class Encounter_handler : MonoBehaviour
{
    public Encounter_Area currentArea;
    public bool encounterTriggered = false;
    public Pokemon wildPokemon;
    public int overworldEncounterChance = 2;
    public static Encounter_handler Instance;
    public event Action<BattleSource> OnEncounterTriggered;

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
        overworldEncounterChance = 2;
        for (int i = 0; i < currentArea.availableEncounters.Length; i++)
        {
            if (EncounteredPokemon(i))
            {
                OnEncounterTriggered?.Invoke(BattleSource.TallGrass);
                break;
            }
        }
    }
    public void TriggerFishingEncounter(Encounter_Area area,Item fishingRod)
    {
        currentArea = area;
        encounterTriggered = true;
        overworldEncounterChance = 2;
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
            if (EncounteredPokemon(i))
            {
                OnEncounterTriggered?.Invoke(BattleSource.Fishing);
                break;
            }
        }
    }
    bool EncounteredPokemon(int currentIndex)
    {
        var random = Utility.RandomRange(1,101);
        var chance = currentArea.availableEncounters[currentIndex].encounterChance;

        if ( currentIndex == currentArea.availableEncounters.Length - 1 /*pick last option if none in range*/ 
             || random < chance )//pick option within chance range
        {
            CreateWildPokemon(currentArea.availableEncounters[currentIndex]);
            return true;
        }
        return false;
    }

    void CreateWildPokemon(EncounterPokemonData pokemonData)
    {
        wildPokemon = Obj_Instance.CreatePokemon(pokemonData.pokemon);
        PokemonOperations.SetPokemonTraits(wildPokemon);
        if (pokemonData.evolutionFormNumber > 0)
        {
            if (pokemonData.evolutionFormNumber > wildPokemon.evolutions.Count)
                Debug.LogError("Evolution number in encounter data is out of range of available evolutions");
            else
                wildPokemon.Evolve(wildPokemon.evolutions[pokemonData.evolutionFormNumber - 1]);
        }
        var randomLevel = Utility.RandomRange(currentArea.minimumLevelOfPokemon, currentArea.maximumLevelOfPokemon);
        var expForRequiredLevel = PokemonOperations.CalculateExpForNextLevel(randomLevel, wildPokemon.expGroup)+1;
        wildPokemon.ReceiveExperience(expForRequiredLevel); 
        wildPokemon.hp=wildPokemon.maxHp;
        StartCoroutine(Battle_handler.Instance.StartWildBattle(wildPokemon));
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
