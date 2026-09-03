using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class PickupTest : BattleBasedTest
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
        testName = "Pickup Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(SetupItemReceival);
    }

    private void SetupItemReceival()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        player.pokemon.heldItem = null;
        player.pokemon.hasItem = false;
        //pickup triggers when the battle ends, which can't be tested using test cases
        //so rather test the method logic individually
        AbilityHandler.CheckItemForPickUpAbility(player.pokemon);
        _sequencer.UseMove();
    }
    
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        _testCaseHandler.AddTestCase("Player must have item",
            () => player.pokemon.hasItem 
            && player.pokemon.heldItem != null);
        
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
                var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
                testingHandler.LogMessage($"Item picked up: {player.pokemon.heldItem.itemName}" ,TestLogType.Information);
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

