using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SemiInvulnerableDoubleBattleTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        /*This test exists to just test that single battle logic doesn't
        break here*/
        testName = "Semi Invulnerability Double Battle Test";

        testExitCondition = TestCompletionCondition.EndManually;
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        
        //fly
        _sequencer.AddAction(() => _sequencer.UseMove());
        //dig
        _sequencer.AddAction(() => _sequencer.UseMoveOnSpecific(
            0,
            BattleParticipantKey.PlayerPartner,
            BattleParticipantKey.EnemyPartner));
        //handle turn logic
        _sequencer.AddAction(EnsureHitAndSkipTurn);
        _sequencer.AddAction(EnsureHitAndSkipTurn);
    }

    private void EnsureHitAndSkipTurn()
    {
        //because of sequence logic, this will always be a player or partner
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        /*semi-invulnerable logic removes sure hit when it's about to
        deal damage, so revert it for testing purposes*/
        currentParticipant.semiInvulnerabilityData.turnData.move.isSureHit = true;
        currentParticipant.semiInvulnerabilityData.turnData.move.priority = 100;
        _turnBasedCombatHandler.NextTurn();
    }
    public override IEnumerator BeginTest()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var enemyPartner = _battleHandler.GetParticipant(BattleParticipantKey.EnemyPartner);
        
        _testCaseHandler.AddTestCase(1,"Enemies were attacked",
            () => enemy.pokemon.hp < enemy.pokemon.maxHp
            && enemyPartner.pokemon.hp < enemyPartner.pokemon.maxHp);

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