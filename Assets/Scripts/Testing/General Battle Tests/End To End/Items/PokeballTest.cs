using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokeballTest: EndToEndTest,IBattleTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private PlayerBagHandler _playerBag;
    private PokemonStorageHandler _pokemonStorageHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        _playerBag.allItems.Clear();
        
        //Avoiding modification of source asset
        var newItem = InstanceFactory.CreateItem(battleTestData.testItems[0].itemAsset);
        newItem.quantity = battleTestData.testItems[0].quantity;
        newItem.dynamicInfoModules = null;
        newItem.dynamicInfoModules = new() { new ItemEffectInfo() };
        _playerBag.AddItem(newItem);
        var pokeball = newItem.GetDynamicModule<ItemEffectInfo>();
        
        AddTestCaseScenario(()=>
        {
            _pokemonStorageHandler.totalPokemonCount = _pokemonStorageHandler.maxPokemonCapacity;
        });
        
        AddTestCaseScenario(()=>
        {
            //ensure escape
            _pokemonStorageHandler.totalPokemonCount = 1;
            pokeball.effectValue = 0;
        });
        AddTestCaseScenario(()=>
        {
            //ensure catch
            pokeball.effectValue = 255;
        });
        
        AddTestCase("Storage size must prevent catching", () => _pokemonPartyHandler.Party.Count==1);
        
        AddTestCase("Player must attempt to catch wild pokemon and fail", () => _pokemonPartyHandler.Party.Count==1);

        AddTestCase("Player must catch wild pokemon", () => _pokemonPartyHandler.Party.Count==2);
        
        this.StartWildBattle(battleTestData,this);
        yield return null;
    }
    protected override void EndTest()
    { 
        //this test makes the player beat the enemy 
        //so no need to manually end battle
    }
    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Pokeball Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _playerBag = container.Resolve<PlayerBagHandler>();
        _pokemonStorageHandler = container.Resolve<PokemonStorageHandler>();
    }
}