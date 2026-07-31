using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsecutiveMoveTest : BattleMoveUsageTest
{
    private BattleHandler _battleHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        testName = "Consecutive Move Test";
    }
    
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        testingHandler.LogMessage($"Health of enemy target: {enemy.pokemon.hp}/{enemy.pokemon.maxHp}",TestLogType.Health);

        var testPassed = enemy.pokemon.hp < enemy.pokemon.maxHp;
        
        SetStatus(testPassed);
    }

    protected override void DetermineTurnUsage()
    {
        if (_battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        {
            //use consecutive move : pin-missile
            var zigzagoonParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            var pinMissile = zigzagoonParticipant.pokemon.moveSet[0];
            pinMissile.isSureHit = true;
            _battleHandler.UseMove(pinMissile,zigzagoonParticipant, BattleParticipantKey.Enemy);
        }
    }
}
