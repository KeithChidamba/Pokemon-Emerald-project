using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionStoneTest: ItemEndToEndTest
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private GameUiHandler _gameUiHandler;

    private string previousName;
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var itemData = (ItemEndToEndTestData)testData;
        LoadItems(itemData.testItems);

        previousName = _pokemonPartyHandler.Party[0].pokemonName;
        
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].requiresEvolutionStone = false;
        });
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].requiresEvolutionStone = true;
            _pokemonPartyHandler.Party[0].evolutionStone = EvolutionStone.LeafStone;
        });
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].requiresEvolutionStone = true;
            _pokemonPartyHandler.Party[0].evolutionStone = EvolutionStone.ThunderStone;
        });
        
        AddTestCase("Evolution should fail due not requiring stone",() => 
            _pokemonPartyHandler.Party[0].pokemonName == previousName);
        
        AddTestCase("Evolution should fail due incorrect stone usage",() => 
            _pokemonPartyHandler.Party[0].pokemonName == previousName);
        
        AddTestCase("Evolution should succeed",() => 
            _pokemonPartyHandler.Party[0].pokemonName != previousName);
        
        _gameUiHandler.ValidateBagView();
        yield return null;
    }

    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Evolution Stone Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
    }
}
