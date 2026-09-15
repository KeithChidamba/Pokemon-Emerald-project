using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveLockTest: EndToEndTest,IBattleTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        AddTestCaseScenario(()=>
        {
            
        });
        
        AddTestCase("Example condition",
            () => _pokemonPartyHandler.Party[0].currentLevel > 0);

        this.StartBattle(battleTestData,this);
        yield return null;
    }
    protected override void EndTest()
    { 
        this.EndBattle();
    }
    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Move Lock Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
    }
}