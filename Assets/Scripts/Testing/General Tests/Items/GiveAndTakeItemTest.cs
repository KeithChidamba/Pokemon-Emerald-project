using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiveAndTakeItemTest: EndToEndTest,IItemTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private GameUiHandler _gameUiHandler;
    private PlayerBagHandler _playerBag;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var itemData = (ItemEndToEndTestData)testData;
        this.LoadItems(itemData.testItems);
       
        AddTestCase(new List<TestCaseCondition>{
            new("Give mudkip a item",
                () => _pokemonPartyHandler.Party[0].hasItem),
            new("Item must be oran berry",
                ()=>_pokemonPartyHandler.Party[0].heldItem.itemName.ToLower() == "oran berry"),
            new("Bag must have 1 berry",
                ()=> _playerBag.allItems[0].quantity==1)
        });
   
        AddTestCase(new List<TestCaseCondition>{
            new("Take mudkip's oran berry",
                () => !_pokemonPartyHandler.Party[0].hasItem),
            new("Bag must have 2 Berries",
                ()=> _playerBag.allItems[0].quantity==2)
        });
        
        _gameUiHandler.ViewPokemonParty(PartyUsage.General);
        
        yield return null;
    }

    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Give And Take Item Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
        _playerBag = container.Resolve<PlayerBagHandler>();
    }
}
