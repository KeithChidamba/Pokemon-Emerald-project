using System.Collections;
using UnityEngine;

public class HealFromWeatherTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        testName = "Heal From Weather Test";
    }
    
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        var playerParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        testingHandler.LogMessage($"Health of player: {playerParticipant.pokemon.hp}" +
                                  $"/{playerParticipant.pokemon.maxHp}",TestLogType.Health);
        
        //initial hp
        var tenthHp = Mathf.FloorToInt(playerParticipant.pokemon.maxHp * 0.1f);
        
        //50% healing when weather isn't a factor
        var baseHealthGain = Mathf.FloorToInt(playerParticipant.pokemon.maxHp * (1f / 2f));
        
        //weather health gain is between (25% and 66%)
        //so this test will never leave pokemon at full hp
        var healthGain = Mathf.FloorToInt(playerParticipant.pokemon.hp - tenthHp);

        testingHandler.LogMessage($"Tenth hp of player: {tenthHp}",TestLogType.Calculation);
        testingHandler.LogMessage($"base health gain: {baseHealthGain}",TestLogType.Calculation);
        testingHandler.LogMessage($"health gained from move: {healthGain}",TestLogType.Calculation);
        
        //this means the weather affected gain
        var testPassed = healthGain > baseHealthGain ||
                         healthGain < baseHealthGain;
        
        SetStatus(testPassed);
    }

    protected override void DetermineTurnUsage()
    {
        //only allow enemy to use a no-damage move
        //so the healing can accurately be accounted for as a test result
        
        if (_battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        {
            _turnBasedCombatHandler.ChangeWeather(new WeatherCondition(Weather.Sunlight));
            
            var wurmpleParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            
            //decrease hp to half so healing is allowed
            wurmpleParticipant.pokemon.hp = Mathf.FloorToInt(wurmpleParticipant.pokemon.maxHp * 0.1f);
            
            //use heal from weather move : moonlight
            var moonlight = wurmpleParticipant.pokemon.moveSet[0];
            _battleHandler.UseMove(moonlight,wurmpleParticipant, BattleParticipantKey.Enemy);
        }
    }
}
