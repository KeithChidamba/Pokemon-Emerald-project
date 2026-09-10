using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvVitaminTest: EndToEndTest,IItemTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private GameUiHandler _gameUiHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var itemData = (ItemEndToEndTestData)testData;
        this.LoadItems(itemData.testItems);
       
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].specialAttackEv = 252;
        });
        AddTestCaseScenario(()=>
        {
            //Maximize ev to trigger test case condition [maxEvTotal = 510]
            var currentPokemon = _pokemonPartyHandler.Party[0];
            currentPokemon.hpEv = 100;
            currentPokemon.attackEv = 100;
            currentPokemon.defenseEv = 100;
            currentPokemon.specialDefenseEv = 100;
            currentPokemon.speedEv = 100;
            
            //normally it would increase [less than 252] but since the total is now 520
            //it should fail
            currentPokemon.specialAttackEv = 20;
        });
        AddTestCaseScenario(()=>
        {
            var currentPokemon = _pokemonPartyHandler.Party[0];
            currentPokemon.hpEv = 100;
            currentPokemon.attackEv = 100;
            currentPokemon.defenseEv = 100;
            currentPokemon.speedEv = 100;
            
            //Allow only 5 evs to be added, vitamin give you 10
            currentPokemon.specialDefenseEv = 95;
            //normally it would increase by 10 but since the total is now 515
            //it should cap out at 15
            currentPokemon.specialAttackEv = 10;
            
            //friendship, < 200 means decrease of 3
            currentPokemon.friendshipLevel = 120;
        });
        AddTestCase("Using calcium vitamin should fail due to ev being maxed[252]",
            () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].specialAttackEv) == 252);
        
        AddTestCase("Using calcium vitamin should fail due to Total ev amount being maxed[510]",
            () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].specialAttackEv) == 20);

        AddTestCase(new List<TestCaseCondition>
        { 
            new("Using calcium vitamin should work, but cap out to make sure the total below 510",
                () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].specialAttackEv) == 15), 
            new("Friendship should decrease by 5 [friendship was manually modified]",
                () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].friendshipLevel) == 117)   
        });
        _gameUiHandler.ValidateBagView();
        yield return null;
    }

    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Ev Vitamin Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
    }
}
