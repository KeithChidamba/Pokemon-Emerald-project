using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EtherTest: EndToEndTest,IItemTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private GameUiHandler _gameUiHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var itemData = (ItemEndToEndTestData)testData;
        this.LoadItems(itemData.testItems);
        
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].moveSet[0].powerpoints = 0;
        });
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].moveSet[0].powerpoints = 0;
        });
        
        AddTestCase("Ether must restore 10 pp",() => 
            _pokemonPartyHandler.Party[0].moveSet[0].powerpoints == 10);
        
        AddTestCase("Ether must restore max pp",() => 
            _pokemonPartyHandler.Party[0].moveSet[0].powerpoints 
            == _pokemonPartyHandler.Party[0].moveSet[0].maxPowerpoints);

        
        _gameUiHandler.ValidateBagView();
        yield return null;
    }

    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Ether Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
    }
}
