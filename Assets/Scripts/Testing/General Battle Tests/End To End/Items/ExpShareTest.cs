using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ExpShareTest: EndToEndTest,IBattleTestable
{
    private BattleHandler _battleHandler;
    private TestingEnvironmentHandler _testingHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    
    private List<(long id,int exp)> expGainedList = new();
    private List<(long id,int exp)> initialExpList = new();

    private long nonExpShareId;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        _battleHandler.OnBattleStarted += SetupHpAndExp;
        void SetupHpAndExp()
        { 
            _battleHandler.OnBattleStarted -= SetupHpAndExp;
            foreach (var pokemon in _pokemonPartyHandler.Party)
            {
                initialExpList.Add(new(pokemon.pokemonID,pokemon.currentExpAmount));
            }
            var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
            enemy.Pokemon.hp = 1;
            //to help test case
            _pokemonPartyHandler.Party[2].moveSet[0].isSureHit = true;
            //trapinch has no exp share
            nonExpShareId = _pokemonPartyHandler.Party[2].pokemonID;
        }
        //Run test case before battle ends
        AddTestCase("Swap to pikachu then trapinch[Attack] then Validate test during battle victory message", ExpGainWorked);
        
        this.StartBattle(battleTestData,this);
        yield return null;
    }
    private bool ExpGainWorked()
    {
        foreach(var initial in initialExpList)
        {
            foreach (var pokemon in _pokemonPartyHandler.Party)
            {
                if(pokemon.pokemonID == initial.id)
                {
                    var diff = pokemon.currentExpAmount - initial.exp;
                    expGainedList.Add(new(initial.id,diff));
                }
            }
        }
        
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var totalExp = enemy.Pokemon.CalculateExperience();
        _testingHandler.LogMessage($"Total exp gain: {totalExp}",TestLogType.Calculation);
        //copied logic from actual exp share logic
        var expShareTotal =  totalExp/ 2;
            
        //there are 2 exp share holder in this test
        var shareExpPerHolder = expShareTotal / 2;
        _testingHandler.LogMessage($"exp after share: {shareExpPerHolder}",TestLogType.Calculation);
         
        foreach(var gained in expGainedList)
        {
            if(gained.id == nonExpShareId)continue;
            
            var projectedExp = gained.exp;
            _testingHandler.LogMessage($"Projected exp gain {projectedExp}",TestLogType.Information);
            
            var passed = projectedExp == shareExpPerHolder;
            if (!passed) return false;
        }
         
        var trapinchData = expGainedList.First(e => e.id == nonExpShareId);
        
        //The non holder of exp share should have half the total
        _testingHandler.LogMessage($"Projected trapinch {trapinchData.exp}",TestLogType.Information);
        
        return expShareTotal == trapinchData.exp;
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