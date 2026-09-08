using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RareCandyTest : ItemEndToEndTest
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private TestingEnvironmentHandler _testHandler;
    private GameUiHandler _gameUiHandler;
    private int expectedLevel;
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var itemData = (ItemEndToEndTestData)testData;
        LoadItems(itemData.testItems);
        
        expectedLevel = _pokemonPartyHandler.Party[0].currentLevel+1;
        
        AddTestCaseScenario(1,()=>
        {
            //nothing important here, just taking advantage
            //of the existing logic to display after test case check
            _testHandler.LogMessage($"Pokemon level: {_pokemonPartyHandler.Party[0].currentLevel}" +
                                    $", expected {expectedLevel}",TestLogType.Information);
        });
        
        AddTestCase("Pokemon must level up by 1",() => 
            _pokemonPartyHandler.Party[0].currentLevel == expectedLevel);
        
        _gameUiHandler.ValidateBagView();
        yield return null;
    }

    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Rare Candy Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _testHandler =  container.Resolve<TestingEnvironmentHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
    }
}
