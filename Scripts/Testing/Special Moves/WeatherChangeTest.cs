using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeatherChangeTest : BattleMoveUsageTest
{
    private Battle_handler _battleHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        testName = "Weather Change Test";
    }
    
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        testingHandler.LogMessage($"The current weather is {_turnBasedCombatHandler.CurrentWeather.weather}",TestLogType.Information);
        
        var testPassed = _turnBasedCombatHandler.CurrentWeather.weather == Weather.Rain;
        
        SetStatus(testPassed);
    }

    protected override void DetermineTurnUsage()
    {
        //only allow enemy to use a no-damage move
        //so the healing can accurately be accounted for as a test result
        
        if (_battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        {
            var mudkipParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            
            //use weather changing move : rain dance
            var rainDance = mudkipParticipant.pokemon.moveSet[0];
            _battleHandler.UseMove(rainDance,mudkipParticipant, BattleParticipantKey.Enemy);
        }
    }
}
