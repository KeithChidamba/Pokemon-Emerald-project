using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FriendshipBerryTest: ItemEndToEndTest
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private GameUiHandler _gameUiHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var itemData = (ItemEndToEndTestData)testData;
        LoadItems(itemData.testItems);
        
        //causes initial item usage to fail
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].friendshipLevel = 255;
        });
        
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].friendshipLevel = 155;
            _pokemonPartyHandler.Party[0].attackEv = 25;
        });
        
        AddTestCase("Friendship should remain the same",() => 
            _pokemonPartyHandler.Party[0].friendshipLevel == 255);
        
        AddTestCase(new List<TestCaseCondition>
        { 
            new("Friendship should increase by 10",
             () => _pokemonPartyHandler.Party[0].friendshipLevel == 165), 
            new("Attack EV should decrease by 10 [Kelpsy berry decreases attack EV]",
             () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].attackEv) == 15)   
        });
        
        _gameUiHandler.ValidateBagView();
        yield return null;
    }

    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Friendship Berry Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
    }
}
