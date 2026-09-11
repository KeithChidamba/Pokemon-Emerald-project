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

        AddTestCase(new List<TestCaseCondition>{
            new("X Attack must give an attack buff", 
                () => player.pokemon.statModifiers.Any(m=>m.stat==Stat.Attack)),
            new ("Attack buff must be stage 1e", 
                ()=>  player.pokemon.statModifiers.Any(m=>m.stage==1))
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