using System.Collections;

public class DamageProtectionMoveTest : BattleMoveUsageTest
{
    private BattleHandler _battleHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        testName = "Damage Protection Move Test";
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
        
        var testPassed = playerParticipant.pokemon.hp >= playerParticipant.pokemon.maxHp;
        
        SetStatus(testPassed);
    }

    protected override void DetermineTurnUsage()
    {
        if (_battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        {
            //use damage protection move : protect
            var zigzagoonParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            var treeckoParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);

            var tackle = treeckoParticipant.pokemon.moveSet[0];
            tackle.isSureHit = true;//make sure it doesnt miss
            
            var protect = zigzagoonParticipant.pokemon.moveSet[0];//protect never misses
            _battleHandler.UseMove(protect,zigzagoonParticipant, BattleParticipantKey.Enemy);
        }
    }
}
