using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiTargetDamageTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private MoveTestActionSequencer _sequencer;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        testName = "Multi Target Damage Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        _sequencer = new MoveTestActionSequencer(container);
        
        _sequencer.AddAction(() => _sequencer.UseMove());//earthquake
        
        _sequencer.AddAction(() => _sequencer.UseMoveOnSpecific(0,
            BattleParticipantKey.PlayerPartner,
            BattleParticipantKey.EnemyPartner));//thunderbolt
        
        _sequencer.AddAction(() => _sequencer.UseMove(1));//surf
    }
    
     public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var enemyPartner = _battleHandler.GetParticipant(BattleParticipantKey.EnemyPartner);
        var partnerParticipant = _battleHandler.GetParticipant(BattleParticipantKey.PlayerPartner);
        testingHandler.LogMessage($"Health of enemy target(Flying Type): {enemy.Pokemon.hp}/{enemy.Pokemon.maxHp}",TestLogType.Health);
        testingHandler.LogMessage($"Health of enemy partner target: {enemyPartner.Pokemon.hp}/{enemyPartner.Pokemon.maxHp}",TestLogType.Health);
        testingHandler.LogMessage($"Health of partner: {partnerParticipant.Pokemon.hp}/{partnerParticipant.Pokemon.maxHp}",TestLogType.Health);
        
        if (_sequencer.SequenceComplete())
        {
           var testPassed = enemy.Pokemon.hp <= enemy.Pokemon.maxHp && 
                             enemyPartner.Pokemon.hp <= enemyPartner.Pokemon.maxHp && 
                             partnerParticipant.Pokemon.hp <= partnerParticipant.Pokemon.maxHp;
            EndTest(testPassed);
        }
    }

    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey is BattleParticipantKey.Enemy or BattleParticipantKey.EnemyPartner) return;
        _sequencer.CallNextAction();
    }
}
