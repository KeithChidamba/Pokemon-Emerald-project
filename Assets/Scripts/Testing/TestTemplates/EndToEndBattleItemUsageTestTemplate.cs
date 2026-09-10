using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndToEndBattleItemUsageTestTemplate: EndToEndTest,IItemTestable,IBattleTestable
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

        this.StartBattle(battleTestData);
        yield return null;
    }

    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "TestNameVariable";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
    }
}