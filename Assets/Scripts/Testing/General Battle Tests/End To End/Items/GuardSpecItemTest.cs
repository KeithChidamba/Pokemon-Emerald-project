using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GuardSpecItemTest: EndToEndTest,IItemTestable,IBattleTestable
{
    private BattleHandler _battleHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        this.LoadItems(battleTestData.testItems);
        
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var playerPartner = _battleHandler.GetParticipant(BattleParticipantKey.PlayerPartner);
        var playerTeam = _battleHandler.GetTeam(BattleParticipantKey.Player);
        
        AddTestCaseScenario(2,()=>
        {
            playerTeam.statChangeEffects.ForEach(s=>s.effectDuration = 0);
        });
        
        AddTestCase(new List<TestCaseCondition>{
            new("Player's team must have immunity to stat decrease", 
                () => player.ProtectedFromStatChange(false)),
            new("Immunity must last 5 turns", 
                () => playerTeam.statChangeEffects.Any(m=>m.effectDuration == 5)),
            new("Player must have no stat decrease after a full turn of attacks from enemy", 
                () => player.pokemon.statModifiers.Count == 0),
            new("Partner must have no stat decrease after a full turn of attacks from enemy", 
                () => playerPartner.pokemon.statModifiers.Count == 0),
        });
        
        AddTestCase("Use Guard Spec to refresh protection", 
                ()=>  playerTeam.statChangeEffects.Any(m=>m.effectDuration == 5));
        
        AddTestCase(new List<TestCaseCondition>{
            new("Player team must have  no immunity", 
                () => playerTeam.statChangeEffects.Count == 0),
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
        testName = "Guard Spec Item Test";
        _battleHandler = container.Resolve<BattleHandler>();
    }
}