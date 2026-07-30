using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SemiInvulnerableDoubleBattleTest : BattleMoveUsageTest
{
    private Battle_handler _battleHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    
    private MoveTestActionSequencer _sequencer;

    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        /*This test exists to just test that single battle logic doesn't
        break here*/
        testName = "Semi Invulnerability Double Battle Test";

        testExitCondition = TestCompletionCondition.EndManually;
        _sequencer = new MoveTestActionSequencer(container);
        
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
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        if (_sequencer.SequenceComplete())
        {
            SetStatus(true);
            EndTest();
        }
    }

    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey is BattleParticipantKey.Enemy or BattleParticipantKey.EnemyPartner)
        {
            return;
        }
        
        if (_sequencer.SequenceComplete()) return;
        _sequencer.CallNextAction();
    }
}