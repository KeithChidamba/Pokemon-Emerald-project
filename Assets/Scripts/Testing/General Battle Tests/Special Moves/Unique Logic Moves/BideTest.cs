using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class BideTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;

    private bool _bideDealtDamage;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Bide Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //enemy use tackle-> bide will work
        SetupBideSequence(0);
        //enemy use tail whip -> bide will fail
        SetupBideSequence(1);
        
        //use on-hit event as proof that bide hit because bide uses complicated turn logic
        //that doesn't allow for generic turn-based test cases handling
        _moveUsageHandler.OnMoveHit += CheckBideHit;
        return;
        void SetupBideSequence(int enemyMoveIndex)
        {
            _sequencer.AddAction(()=>
            {
                _bideDealtDamage = false;
                var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
                enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
                enemy.pokemonTrainerAI.AssignBehaviorAction(() =>
                {
                    enemy.pokemon.moveSet[enemyMoveIndex].isSureHit = true;
                    _battleHandler.UseMove(enemy.pokemon.moveSet[enemyMoveIndex], enemy, BattleParticipantKey.Player);
                });
                var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
                player.pokemon.moveSet[0].priority = 100;
                _sequencer.UseMove();//Bide
            });
            _sequencer.AddAction(()=>{});//cooldown turn buffer
            _sequencer.AddAction(()=>{});//cooldown turn buffer
            //test for bide's interference with normal move usage
            _sequencer.AddAction(()=>_sequencer.UseMove(1));
        }
    }
    private void CheckBideHit(BattleParticipant attacker,BattleParticipant victim,Move moveUsed,float finalDamage)
    {
        if (attacker.participantKey != BattleParticipantKey.Player) return;
        if (NameDB.ParseMoveName(moveUsed.moveName) == MoveName.Bide)
        {
            _bideDealtDamage = true;
        }
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        _testCaseHandler.AddTestCase(0,"Bide should be activated",
            () => NameDB.ParseMoveName(player.currentCoolDown.turnData.move.moveName) == MoveName.Bide);
        
        _testCaseHandler.AddTestCase(3,"Bide should hit enemy", () => _bideDealtDamage);
        
        _testCaseHandler.AddTestCase(4,"Bide should be activated",
            () => NameDB.ParseMoveName(player.currentCoolDown.turnData.move.moveName) == MoveName.Bide);
        
        _testCaseHandler.AddTestCase(7,"Bide should not hit enemy", () => !_bideDealtDamage);
        
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
            //prevent fainting for test reliability
            var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy); 
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            player.pokemon.hp = player.pokemon.maxHp;
            enemy.pokemon.hp = enemy.pokemon.maxHp;
            
            if (_sequencer.SequenceComplete())
            {
                _moveUsageHandler.OnMoveHit -= CheckBideHit;
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

