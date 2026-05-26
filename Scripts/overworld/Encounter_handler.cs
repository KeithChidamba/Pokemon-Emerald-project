using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public enum BattleEncounterSource
{
    None,Fishing,NormalEncounter
}
public class Encounter_handler : MonoBehaviour,IInjectable
{
    public event Action<Pokemon,BattleEncounterSource> OnEncounterTriggered;
    private PokemonOperations _pokemonOperationsHandler;
    private Battle_handler _battleHandler;
    public void Inject(ServiceContainer container)
    {
        _battleHandler = container.Resolve<Battle_handler>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        gameObject.SetActive(true);
    }
    
    public void OnInject()
    {
        
    }
    public void TriggerEncounter(NormalEncounteArea table)
    {
        DeterminePossibleEncounter(
            table.data, 
            table.data.availableEncounters.Length,
            table.biome
            ,BattleEncounterSource.NormalEncounter);
    }
    public void TriggerFishingEncounter(FishingEncounterTable table,Item fishingRod)
    {
        //the type of rod determines available pokemon from pool
        var formattedRodName = fishingRod.itemName.Replace(" ", "");
        
        var tableForRod = table.fishingTables.First(t => t.rodType == Enum.Parse<RodType>(formattedRodName));
        
        DeterminePossibleEncounter(
            tableForRod.tableData, 
            tableForRod.tableData.availableEncounters.Length,
            table.biome,BattleEncounterSource.Fishing);
    }

    private void DeterminePossibleEncounter(EncounterTableData tableData,int numAvailableEncounters,Biome biome,BattleEncounterSource source)
    {
        for (int i = 0; i < numAvailableEncounters; i++)
        {
            var random = Utility.RandomRange(1,101);
            var chance = tableData.availableEncounters[i].encounterChance;

            if ( i == tableData.availableEncounters.Length - 1 /*pick last option if none in range*/ 
                 || random < chance )//pick option within chance range
            {
                CreateWildPokemon(tableData.availableEncounters[i],tableData, biome,source);
                break;
            }
        }
    }
    private void CreateWildPokemon(EncounterPokemonData pokemonData,EncounterTableData tableData,Biome biome,BattleEncounterSource source)
    {
        var wildPokemon = InstanceFactory.CreatePokemon(pokemonData.pokemon);
        OnEncounterTriggered?.Invoke(wildPokemon,source);
        _pokemonOperationsHandler.SetPokemonTraits(wildPokemon);
        if (pokemonData.evolutionFormNumber > 0)
        {
            if (pokemonData.evolutionFormNumber > wildPokemon.evolutions.Count)
                Debug.LogError("Evolution number in encounter data is out of range of available evolutions");
            else
                wildPokemon.Evolve(wildPokemon.evolutions[pokemonData.evolutionFormNumber - 1]);
        }
        var randomLevel = Utility.RandomRange(tableData.minimumLevelOfPokemon, tableData.maximumLevelOfPokemon);
        var expForRequiredLevel = PokemonOperations.CalculateExpForNextLevel(randomLevel, wildPokemon.expGroup)+1;
        wildPokemon.canEvolve = false;//prevent evolution from exp
        wildPokemon.ReceiveExperience(expForRequiredLevel); 
        wildPokemon.hp=wildPokemon.maxHp;
        StartCoroutine(_battleHandler.StartWildBattle(wildPokemon,biome));
    }
}
