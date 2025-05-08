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
    public void Trigger_encounter(Encounter_Area area)
    {
        currentArea = area;
        encounterTriggered = true;
        encounterChance = 2;
        for (int i = 0; i < currentArea.availablePokemon.Length; i++)
        {
            var random = Utility.RandomRange(1,101);
            var chance = int.Parse(currentArea.availablePokemon[i].Substring(currentArea.availablePokemon[i].Length - 3, 3));
            if ( (i == currentArea.availablePokemon.Length - 1) /*pick last option if none in range*/ || (random < chance) )//pick option within chance range
            {
                var pokemonName = currentArea.availablePokemon[i].Substring(0, currentArea.availablePokemon[i].Length - 3);
                CreateWildPokemon(pokemonName);
                break;
            }
        }
        
    }
    void CreateWildPokemon(string pokemonName)
    {
        wildPokemon = Obj_Instance.set_Pokemon(Resources.Load<Pokemon>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + pokemonName.ToLower()+"/"+ pokemonName.ToLower()));
        if (wildPokemon != null)
        {
            PokemonOperations.SetPkmtraits(wildPokemon);
            var randomLevel = Utility.RandomRange(currentArea.minimumLevelOfPokemon, currentArea.maximumLevelOfPokemon+1);
            var expForRequiredLevel = PokemonOperations.GetNextLv(randomLevel - 1, wildPokemon.EXPGroup)+1;
            wildPokemon.ReceiveExperience(expForRequiredLevel);
            wildPokemon.HP=wildPokemon.max_HP;
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
