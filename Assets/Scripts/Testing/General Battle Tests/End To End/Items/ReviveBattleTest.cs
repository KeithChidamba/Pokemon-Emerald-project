using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReviveBattleTest: EndToEndTest,IItemTestable,IBattleTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
    
        this.LoadItems(battleTestData.testItems);
        
        AddTestCaseScenario(()=>
        {
            //guarantee faint
            _pokemonPartyHandler.Party[1].hp = 1;
            _pokemonPartyHandler.Party[1].statusEffect = StatusEffect.Burn;
        });
        
        AddTestCase("partner pokemon must faint (by burn or getting attacked)",
            () => _pokemonPartyHandler.Party[1].hp <=0 );

        AddTestCase("partner pokemon must be revived",
            () => _pokemonPartyHandler.Party[1].hp > 0 );
        
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
        testName = "Revive Battle Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
    }
}