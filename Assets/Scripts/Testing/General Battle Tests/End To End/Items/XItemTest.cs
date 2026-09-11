using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class XItemTest: EndToEndTest,IItemTestable,IBattleTestable
{
    private BattleHandler _battleHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        this.LoadItems(battleTestData.testItems);
        
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);

        AddTestCaseScenario(1,()=>
        {
            player.pokemon.statModifiers[0].isAtLimit = true;
        });
        
        AddTestCase(new List<TestCaseCondition>{
            new("X Attack must give an attack buff", 
                () => player.pokemon.statModifiers.Any(m=>m.stat==Stat.Attack)),
            new ("Attack buff must be stage 1", 
                ()=>  player.pokemon.statModifiers.Any(m=>m.stage==1))
        });
        AddTestCase(new List<TestCaseCondition>{
            new("X Attack must not give an attack buff", 
                () => player.pokemon.statModifiers.Any(m=>m.stat==Stat.Attack)),
            new ("Attack buff must be at limit", 
                ()=>  player.pokemon.statModifiers.Any(m=>m.isAtLimit))
        });
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
        testName = "X Item Test";
        _battleHandler = container.Resolve<BattleHandler>();
    }
}