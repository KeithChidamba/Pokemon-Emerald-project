using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class HealthDrainTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        testName = "Health Drain Test";
    }
    
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var playerParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        var halfHp = Mathf.FloorToInt(playerParticipant.Pokemon.maxHp / 2f);
        
        testingHandler.LogMessage($"Health of enemy target: {enemy.Pokemon.hp}/{enemy.Pokemon.maxHp}",TestLogType.Health);
        testingHandler.LogMessage($"Half Health of player: {halfHp}",TestLogType.Calculation);
        testingHandler.LogMessage($"Health of player: {playerParticipant.Pokemon.hp}" +
                                  $"/{playerParticipant.Pokemon.maxHp}",TestLogType.Health);
        
        var testPassed = enemy.Pokemon.hp < enemy.Pokemon.maxHp
            && playerParticipant.Pokemon.hp > halfHp;
        
        SetTestStatus(testPassed);
    }

    protected override void DetermineTurnUsage()
    {
        //only allow enemy to use a no-damage move
        //so the healing can accurately be accounted for as a test result
        
        if (_battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        {
            //use health drain move : leech-life
            var zubatParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            
            zubatParticipant.Pokemon.hp = Mathf.FloorToInt(zubatParticipant.Pokemon.maxHp / 2f);
            
            var leechLife = zubatParticipant.Pokemon.moveSet[0];
            leechLife.isSureHit = true;
            _battleHandler.UseMove(leechLife,zubatParticipant, BattleParticipantKey.Enemy);
        }
    }
}
