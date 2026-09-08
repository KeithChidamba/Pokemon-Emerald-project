using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndToEndItemTestTesmplate: ItemEndToEndTest
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private TestingEnvironmentHandler _testHandler;
    private GameUiHandler _gameUiHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var itemData = (ItemEndToEndTestData)testData;
        LoadItems(itemData.testItems);
        
        AddTestCaseScenario(()=>
        {
            //example scenario
        });
        
        AddTestCase("Example condition",() => 
            _pokemonPartyHandler.Party[0].currentLevel == 1);
        
        _gameUiHandler.ValidateBagView();
        yield return null;
    }

    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "TestNameVariable";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _testHandler =  container.Resolve<TestingEnvironmentHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
    }
}
