using System.Collections;
 
public class PursuitTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
       
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Pursuit Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //pursuit should defeat enemy
        _sequencer.AddAction(ForceEnemySwitch);
        _sequencer.AddAction(SwapPlayerToTriggerPursuit);
        _sequencer.AddAction(() => _sequencer.UseMove());
    }

    private void SwapPlayerToTriggerPursuit()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Natural);
        _pokemonPartyHandler.SwapToPartner();
        //enemy only has pursuit so that will trigger
    }
    private void ForceEnemySwitch()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySwap);
        
        //pursuit
        _sequencer.UseMove();
        return;
        void ForceEnemySwap()
        {
            enemy.pokemonTrainerAI.SwitchPokemon(1);
        }
    }
    public override IEnumerator BeginTest()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        _testCaseHandler.AddTestCase(0,"Pursuit must faint previous enemy on switch",
            () => enemy.pokemonTrainerAI.trainerParty[0].hp <= 0);
        
        _testCaseHandler.AddTestCase(1,"Pursuit must hit player on switch",
            () => _pokemonPartyHandler.Party[1].hp < _pokemonPartyHandler.Party[1].maxHp);
        
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

