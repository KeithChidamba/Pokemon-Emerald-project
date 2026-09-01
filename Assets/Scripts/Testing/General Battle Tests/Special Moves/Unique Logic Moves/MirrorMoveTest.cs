using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class MirrorMoveTest : BattleBasedTest
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
        testName = "Mirror Move Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(() =>
        {
            //should fail, mirror can't copy when there's no previous move
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            player.pokemon.moveSet[0].priority = 100;
            _sequencer.UseMove();
        });
        
        //enemy use detect
        _sequencer.AddAction(()=>MakeEnemyUseInvalidMove(0));
        //enemy use protect
        _sequencer.AddAction(()=>MakeEnemyUseInvalidMove(1));
        //enemy use haze
        _sequencer.AddAction(()=>MakeEnemyUseInvalidMove(2));
        //bulk-up [self target moves can't be copied]
        _sequencer.AddAction(()=>MakeEnemyUseInvalidMove(3));
        //tackle
        _sequencer.AddAction(GiveEnemyNormalMove);
        //tailwhip used by player
        _sequencer.AddAction(AttackNormally);
    }

    private void AttackNormally()
    {
        //this action is to make sure mirror move doesn't interfere with regular moves after being used
        //enemy is still in control mode, so it will use tackle
        _sequencer.UseMove(1);
    }
    private void GiveEnemyNormalMove()
    {
        var moveName = NameDB.GetMoveName(MoveName.Tackle);
        var assetPath = DirectoryHandler.GetDirectory(AssetDirectory.Moves) + moveName;
        var moveFromAsset = Resources.Load<Move>(assetPath);
        var newMove = InstanceFactory.CreateMove(moveFromAsset);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemon.moveSet.RemoveAt(0);
        enemy.pokemon.moveSet.Add(newMove);
        //make enemy use tackle which can be copied
        MakeEnemyUseInvalidMove(3);
    }
    private void MakeEnemyUseInvalidMove(int moveIndex)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseSpecificMove);
        
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        player.pokemon.moveSet[0].priority = 0;//ensure mirror move happens after enemy attack
        _sequencer.UseMove();
        return;
        void UseSpecificMove()
        {
            //modified for test case reliability
            enemy.pokemon.moveSet[moveIndex].priority = 100;
            enemy.pokemon.moveSet[moveIndex].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet[moveIndex], enemy, BattleParticipantKey.Player);
        }
    }
    public override IEnumerator BeginTest()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Mirror move should fail due to no previous move being used by enemy" +
                " and enemy should be un-harmed", ()=> enemy.pokemon.hp >= enemy.pokemon.maxHp),
        });
        
        var nonCopyableMoves = new[] {MoveName.Detect,MoveName.Protect,MoveName.Haze};
        for (int i = 0; i < 3; i++)
        {
            var index = i;
            _testCaseHandler.AddTestCase(new List<TestCaseCondition>
            {
                new("Enemy should be un-harmed", ()=> enemy.pokemon.hp >= enemy.pokemon.maxHp),
                new($"Mirror move should fail due to invalid previous move({nonCopyableMoves[index]}) used by enemy",
                    ()=> NameDB.ParseMoveName(enemy.previousMoveData.move.moveName)
                         == nonCopyableMoves[index])
            });
        }
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Enemy should be un-harmed", ()=> enemy.pokemon.hp >= enemy.pokemon.maxHp),
            new("Mirror move should fail due to self-targeting previous move (Bulk up) used by enemy",
                ()=> NameDB.ParseMoveName(enemy.previousMoveData.move.moveName) == MoveName.BulkUp)
        });
        
        _testCaseHandler.AddTestCase("Enemy should be hurt because Player copied tackle from enemy"
            , ()=> enemy.pokemon.hp < enemy.pokemon.maxHp);
        
        _testCaseHandler.AddTestCase("Player used regular move tailwhip",
                ()=> NameDB.ParseMoveName(player.previousMoveData.move.moveName) == MoveName.TailWhip);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
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
            //add extra logic here
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

