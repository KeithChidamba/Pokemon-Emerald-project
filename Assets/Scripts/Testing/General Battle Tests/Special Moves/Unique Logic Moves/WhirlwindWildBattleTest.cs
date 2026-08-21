using System.Collections;
 
public class WhirlwindWildBattleTest : WildBattleBasedTest
{
    private BattleHandler _battleHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private MoveTestActionSequencer _sequencer;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        testName = "Whirlwind Wild Battle Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        _turnBasedCombatHandler.OnTurnsCompleted += EndTestAfterBattleTermination;
        //whirlwind-> end battle
        _sequencer.AddAction(AttackFirst);
    }

    private void EndTestAfterBattleTermination()
    {
        _turnBasedCombatHandler.OnTurnsCompleted -= EndTestAfterBattleTermination;
        testingHandler.LogMessage($"Test PASSED due to battle termination case", TestLogType.TestCase);
        EndTest(true);
    }
    private void AttackFirst()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        enemy.canEscape = false;
        player.pokemon.moveSet[0].priority = 100;
        _sequencer.UseMove();
    }
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
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

