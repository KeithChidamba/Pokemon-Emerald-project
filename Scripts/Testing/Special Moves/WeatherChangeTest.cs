using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeatherChangeTest : BattleMoveUsageTest
{
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        testName = "Weather Change Test";
    }
    
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        testingHandler.LogMessage($"The current weather is {turnBasedCombatHandler.CurrentWeather.weather}",TestLogType.Information);
        
        var testPassed = turnBasedCombatHandler.CurrentWeather.weather == Weather.Rain;
        
        SetStatus(testPassed);
    }

    protected override void DetermineMoveUsage()
    {
        //only allow enemy to use a no-damage move
        //so the healing can accurately be accounted for as a test result
        
        if (battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        {
            var mudkipParticipant = battleHandler.GetParticipant(BattleParticipantKey.Player);
            
            //use weather changing move : rain dance
            var rainDance = mudkipParticipant.pokemon.moveSet[0];
            battleHandler.UseMove(rainDance,mudkipParticipant, BattleParticipantKey.Enemy);
        }
    }
}
