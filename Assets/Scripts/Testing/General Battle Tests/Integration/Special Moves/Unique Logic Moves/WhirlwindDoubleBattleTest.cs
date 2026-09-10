using System;
using System.Collections;
using System.Collections.Generic;

public class WhirlwindDoubleBattleTest : BattleBasedTest
{
    private BattleHandler _battleHandler;

    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    private long _currentEnemyID;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
       
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Whirlwind Double Battle Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        //whirlwind[should fail] -> tailwhip
        _sequencer.AddAction(()=>_sequencer.UseMove());
        //whirlwind from team-mate [should fail] -> tailwhip
        _sequencer.AddAction(() => _sequencer.UseMoveOnSpecific(0,
            BattleParticipantKey.PlayerPartner,
            BattleParticipantKey.Player));
         
        //tackle[should faint enemy] -> tailwhip
        _sequencer.AddAction(ForceEnemyFaint);
        //whirlwind on next enemy[should work] -> tailwhip
        _sequencer.AddAction(PartnerUseWhirlwind);
         
        //tackle[should faint enemy] -> tailwhip
        _sequencer.AddAction(ForceEnemyFaint);
        //whirlwind[should fail -  no more available swaps] -> tailwhip
        _sequencer.AddAction(PartnerUseWhirlwind);
    }

    private void PartnerUseWhirlwind()
    {
        var enemyPartner = _battleHandler.GetParticipant(BattleParticipantKey.EnemyPartner);
        _currentEnemyID = enemyPartner.pokemon.pokemonID;
        _sequencer.UseMoveOnSpecific(0,
            BattleParticipantKey.PlayerPartner,
            BattleParticipantKey.EnemyPartner);
    }
    private void ForceEnemyFaint()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        //ensure faint
        enemy.pokemon.hp = 2;
        //tackle
        _sequencer.UseMove(1);
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemyPartner = _battleHandler.GetParticipant(BattleParticipantKey.EnemyPartner);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase("Whirlwind should fail due to level gap," +
                                     "partner's whirlwind should fail as well",
            () => player.pokemon.currentLevel < enemy.pokemon.currentLevel);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Enemy was fainted",
                () => enemy.pokemonTrainerAI.GetLivingPokemonCount() < enemy.pokemonTrainerAI.TrainerParty.Count),
            new("Partner successfully used whirlwind",
                () => enemyPartner.pokemon.pokemonID != _currentEnemyID)
        });

        _testCaseHandler.AddTestCase("Whirlwind should fail",
            () => _currentEnemyID == enemyPartner.pokemon.pokemonID);
        
        //for testing purposes, disable the switch style
        _battleHandler.SetBattleStyle((int)BattleHandler.BattlesStyle.Set);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        _testCaseHandler.HandleCurrentTestCase(CheckTestEnd,TestCaseFailed);
        
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        testingHandler.LogMessage($"Count living: {enemy.pokemonTrainerAI.GetLivingPokemonCount()} " +
                                  $"-> current: {enemy.pokemonTrainerAI.TrainerParty.Count}", TestLogType.Information);
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

