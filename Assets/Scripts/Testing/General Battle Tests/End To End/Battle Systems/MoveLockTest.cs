using System.Collections;
using System.Collections.Generic;

public class MoveLockTest: EndToEndTest,IBattleTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private BattleHandler _battleHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        AddTestCaseScenario(()=>
        {
            //make sure test pokemon is guaranteed to attack
            _pokemonPartyHandler.Party[0].moveSet[0].isSureHit = true;
        });
        
        AddTestCase(new List<TestCaseCondition>
        {
            new("Check if player used tackle", 
                ()=>NameDB.NameMatch(player.previousMoveData.move,MoveName.Tackle)),
            new("Choice band locked move",()=>player.currentMoveLock.moveLocked),
            new("Check if the locked move is tackle",
                () => NameDB.NameMatch(player.currentMoveLock.moveToLock,MoveName.Tackle)),
        });
        AddTestCase("Choice band effect should be removed after switch", () => !player.currentMoveLock.moveLocked);

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
        _battleHandler = container.Resolve<BattleHandler>();
    }
}