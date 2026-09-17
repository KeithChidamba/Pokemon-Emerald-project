using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpShareTest: EndToEndTest,IBattleTestable
{
    private BattleHandler _battleHandler;
    private TestingEnvironmentHandler _testingHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    
    private List<int> expGainedList = new();
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        _battleHandler.OnBattleStarted += SetupHpAndExp;
        void SetupHpAndExp()
        { 
            _battleHandler.OnBattleStarted -= SetupHpAndExp;
            
            foreach (var pokemon in _pokemonPartyHandler.Party)
            {
                pokemon.OnExpGained += exp =>
                {
                    expGainedList.Add(exp);
                };
            }
            
            var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
            enemy.pokemon.hp = 1;
            //to help test case
            _pokemonPartyHandler.Party[2].moveSet[0].isSureHit = true;
        }
        //Run test case before battle ends
        AddTestCase("Exp gained = base exp gain * modifier", ExpGainWorked);
        
        this.StartBattle(battleTestData,this);
        yield return null;
    }
    private bool ExpGainWorked()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var totalExp = enemy.pokemon.CalculateExperience();
        _testingHandler.LogMessage($"Total exp gain: {totalExp}",TestLogType.Calculation);
        //copied logic from actual exp share logic
        var expShareTotal =  totalExp/ 2;
            
        //there are 2 exp share holder in this test
        var shareExpPerHolder = expShareTotal / 2;
         _testingHandler.LogMessage($"exp after share: {shareExpPerHolder}",TestLogType.Calculation);
         
        for (var i=0;i<2;i++)
        {
            var projectedExp = expGainedList[i];
            _testingHandler.LogMessage($"Projected exp gain {i+1} {projectedExp}",TestLogType.Information);
            
            var passed = projectedExp == shareExpPerHolder;
            if (!passed) return false;
        }
        
        //last pokemon is the non holder of exp share
        //so it's exp should be half the total
        _testingHandler.LogMessage($"Projected exp gain 3 {expGainedList[2]}",TestLogType.Information);
        return expShareTotal == expGainedList[2];
    }
    protected override void EndTest()
    { 
        //this test makes the player beat the enemy 
        //so no need to manually end battle
    }
    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Exp Share Test";
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _testingHandler = container.Resolve<TestingEnvironmentHandler>();
    }
}