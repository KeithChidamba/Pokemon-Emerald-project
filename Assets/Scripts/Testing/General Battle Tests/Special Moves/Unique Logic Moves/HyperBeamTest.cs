using System.Collections;

public class HyperBeamTest : BattleBasedTest
{
    private BattleHandler _battleHandler;

    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Hyper Beam Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(AttackWithHyperBeam);
        _sequencer.AddAction(()=>{});//cooldown turn buffer
        //attack after hyper beam cooldown [tailwhip]
        _sequencer.AddAction(()=>_sequencer.UseMove(1));
    }

    private void AttackWithHyperBeam()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        player.pokemon.moveSet[0].priority = 100;
        _sequencer.UseMove();
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy); 
        
        _testCaseHandler.AddTestCase(0,"hyperBeam should hit enemy",
            () => enemy.pokemon.hp < enemy.pokemon.maxHp);
        
        _testCaseHandler.AddTestCase(2,"Player cooldown down should be over, and player should use tailwhip",
            () =>NameDB.ParseMoveName(player.previousMoveData.move.moveName) == MoveName.TailWhip);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        var caseExists = _testCaseHandler.CheckForCurrentTestCase(CheckTestEnd,TestCaseFailed);
        if (!caseExists)
        {
            CheckTestEnd();
        }
        return;
        void CheckTestEnd()
        {
            if (_sequencer.SequenceComplete())
            {
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            EndTest(false);
        }
    }
    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey is BattleParticipantKey.Enemy or BattleParticipantKey.EnemyPartner)
        {
            return;
        }
        _sequencer.CallNextAction();
    }
}

