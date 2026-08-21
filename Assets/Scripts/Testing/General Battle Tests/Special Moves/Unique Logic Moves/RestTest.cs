using System.Collections;

public class RestTest : BattleBasedTest
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
        testName = "Rest Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //Rest,should fail -> enemy uses flamethrower
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
        //Rest,should work -> enemy uses tail whip
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,1));
    }

    private void ForceEnemyMoveAndAttack(int moveIndex,int enemyMoveIndex)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);

        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseSpecificMove);
        
        player.pokemon.moveSet[moveIndex].priority = 100;
        _sequencer.UseMove(moveIndex);
        return;
        void UseSpecificMove()
        {
            enemy.pokemon.moveSet[enemyMoveIndex].statusChance = 100;
            enemy.pokemon.moveSet[enemyMoveIndex].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet[enemyMoveIndex], enemy, BattleParticipantKey.Player);
        }
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        _testCaseHandler.AddTestCase("Player must get hit by enemy",
            () => player.pokemon.hp < player.pokemon.maxHp);
        
        _testCaseHandler.AddTestCase("Player be healed by rest, and asleep",
            () => player.pokemon.hp >= player.pokemon.maxHp
            && player.pokemon.statusEffect == StatusEffect.Sleep);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        testingHandler.LogMessage($"Health of enemy: {enemy.pokemon.hp}" +
                                  $"/{enemy.pokemon.maxHp}",TestLogType.Health);
        testingHandler.LogMessage($"Health of player: {player.pokemon.hp}" +
                                  $"/{player.pokemon.maxHp}",TestLogType.Health);

        _testCaseHandler.HandleCurrentTestCase(CheckTestEnd,TestCaseFailed);
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

