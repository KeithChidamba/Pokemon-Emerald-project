using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokeballTest: EndToEndTest,IItemTestable,IBattleTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        this.LoadItems(battleTestData.testItems);
        
        AddTestCaseScenario(()=>
        {
            
        });
        
        AddTestCase("Example condition",
            () => _pokemonPartyHandler.Party[0].currentLevel == 1);

        this.StartWildBattle(battleTestData,this);
        yield return null;
    }
    protected override void EndTest()
    { 
        this.EndBattle();
    }
    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Pokeball Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
    }
}