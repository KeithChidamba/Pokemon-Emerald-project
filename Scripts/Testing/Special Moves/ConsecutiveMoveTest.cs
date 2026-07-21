using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsecutiveMoveTest : BattleMoveUsageTest
{
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        testName = "Consecutive Move Test";
    }
    
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        var enemy = battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        testingHandler.LogMessage($"Health of enemy target: {enemy.pokemon.hp}/{enemy.pokemon.maxHp}",TestLogType.Health);

        var testPassed = enemy.pokemon.hp < enemy.pokemon.maxHp;
        
        SetStatus(testPassed);
    }

    protected override void DetermineMoveUsage()
    {
        if (battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        {
            //use consecutive move : pin-missile
            var zigzagoonParticipant = battleHandler.GetParticipant(BattleParticipantKey.Player);
            var pinMissile = zigzagoonParticipant.pokemon.moveSet[0];
            pinMissile.isSureHit = true;
            battleHandler.UseMove(pinMissile,zigzagoonParticipant, BattleParticipantKey.Enemy);
        }
    }
}
