using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class HealthDrainTest : BattleMoveUsageTest
{
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        testName = "Health Drain Test";
    }
    
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        var enemy = battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var playerParticipant = battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        var halfHp = Mathf.FloorToInt(playerParticipant.pokemon.maxHp / 2f);
        
        testingHandler.LogMessage($"Health of enemy target: {enemy.pokemon.hp}/{enemy.pokemon.maxHp}",LogType.Health);
        testingHandler.LogMessage($"Half Health of player: {halfHp}",LogType.Calculation);
        testingHandler.LogMessage($"Health of player: {playerParticipant.pokemon.hp}" +
                                  $"/{playerParticipant.pokemon.maxHp}",LogType.Health);
        
        var testPassed = enemy.pokemon.hp < enemy.pokemon.maxHp
            && playerParticipant.pokemon.hp > halfHp;
        
        SetStatus(testPassed);
    }

    protected override void DetermineMoveUsage()
    {
        //only allow enemy to use a no-damage move
        //so the healing can accurately be accounted for as a test result
        
        if (battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        {
            //use health drain move : leech-life
            var zubatParticipant = battleHandler.GetParticipant(BattleParticipantKey.Player);
            
            zubatParticipant.pokemon.hp = Mathf.FloorToInt(zubatParticipant.pokemon.maxHp / 2f);
            
            var leechLife = zubatParticipant.pokemon.moveSet[0];
            leechLife.isSureHit = true;
            battleHandler.UseMove(leechLife,zubatParticipant, BattleParticipantKey.Enemy);
        }
    }
}
