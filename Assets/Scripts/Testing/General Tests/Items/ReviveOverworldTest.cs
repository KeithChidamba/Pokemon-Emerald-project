using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReviveOverworldTest: EndToEndTest,IItemTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private GameUiHandler _gameUiHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var itemData = (ItemEndToEndTestData)testData;
        this.LoadItems(itemData.testItems);
        
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].hp = 0;
        });
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].hp = 0;
        });
        
        AddTestCase("Pokemon must be revived",() => 
            _pokemonPartyHandler.Party[0].hp > 0);
        
        AddTestCase("Pokemon must be revived and have max hp",() => 
            Mathf.FloorToInt(_pokemonPartyHandler.Party[0].hp) == Mathf.FloorToInt(_pokemonPartyHandler.Party[0].maxHp));
        
        _gameUiHandler.ValidateBagView();
        yield return null;
    }

    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Revive Overworld Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
    }
}
