using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonusExpGainTest: EndToEndTest,IBattleTestable
{
    private BattleHandler _battleHandler;
    private TestingEnvironmentHandler _testingHandler;
    
    private int expGained;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        _battleHandler.OnBattleStarted += SetupHpAndExp;
        void SetupHpAndExp()
        { 
            _battleHandler.OnBattleStarted -= SetupHpAndExp;
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            player.pokemon.OnExpGained += exp =>
            {
                expGained = exp;
            };
            var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
            enemy.pokemon.hp = 1;
        }
        //Run test case before battle ends
        AddTestCase("Exp gained = base exp gain * modifier", ExpGainWorked);
        
        this.StartBattle(battleTestData,this);
        yield return null;
    }

    private bool ExpGainWorked()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var gain = enemy.pokemon.CalculateExperience();
        var result = Mathf.FloorToInt(player.pokemon.AccountForExpGainModifier() * gain);
        _testingHandler.LogMessage($"base exp gain: {gain}",TestLogType.Calculation);
        _testingHandler.LogMessage($"exp after bonus gain: {result}",TestLogType.Calculation);
        _testingHandler.LogMessage($"Projected exp gain {expGained}",TestLogType.Information);
        return expGained == result;
    }
    protected override void EndTest()
    { 
        //this test makes the player beat the enemy 
        //so no need to manually end battle
    }
    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Bonus Exp Gain Test";
        _battleHandler = container.Resolve<BattleHandler>();
        _testingHandler = container.Resolve<TestingEnvironmentHandler>();
    }
}