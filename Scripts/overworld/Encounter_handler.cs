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
            table.biome
            ,BattleEncounterSource.NormalEncounter);
    }
    public void TriggerFishingEncounter(FishingEncounterTable table,Item fishingRod)
    {
        //the type of rod determines available pokemon from pool
        var rodType = fishingRod.GetDynamicModule<FishingRodInfo>().fishingRodType;
        var tableForRod = table.fishingTables.First(t => t.rodType == rodType);
        
        DeterminePossibleEncounter(
            tableForRod.tableData, 
            table.biome,BattleEncounterSource.Fishing);
    }

    private void DeterminePossibleEncounter(EncounterTableData tableData,Biome biome,BattleEncounterSource source)
    {
        var totalWeight= tableData.availableEncounters.Sum(enc=>enc.encounterChance);
        
        int roll = Utility.RandomRange(0, totalWeight);

        foreach (var encounter in tableData.availableEncounters)
        {
            if (roll < encounter.encounterChance)
            {
                CreateWildPokemon(encounter, tableData, biome, source);
                return;
            }
            roll -= encounter.encounterChance;
        }
    }
    private void CreateWildPokemon(EncounterPokemonData pokemonData,EncounterTableData tableData,Biome biome,BattleEncounterSource source)
    {
        var randomLevel = Utility.RandomRange(tableData.minimumLevelOfPokemon, tableData.maximumLevelOfPokemon);
        
        _pokemonOperationsHandler.CreateSpecificPokemon(StartBattle,pokemonData.pokemon,randomLevel,pokemonData.evolutionFormNumber);
        void StartBattle(Pokemon wildPokemon)
        {
            OnEncounterTriggered?.Invoke(wildPokemon,source);
            StartCoroutine(_battleHandler.StartWildBattle(wildPokemon,biome));
        }
    }
}
