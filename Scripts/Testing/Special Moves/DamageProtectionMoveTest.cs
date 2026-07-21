using System.Collections;

public class DamageProtectionMoveTest : BattleMoveUsageTest
{
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        testName = "Damage Protection Move Test";
    }
    
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        var playerParticipant = battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        testingHandler.LogMessage($"Health of player: {playerParticipant.pokemon.hp}" +
                                  $"/{playerParticipant.pokemon.maxHp}",LogType.Health);
        
        var testPassed = playerParticipant.pokemon.hp >= playerParticipant.pokemon.maxHp;
        
        SetStatus(testPassed);
    }

    protected override void DetermineMoveUsage()
    {
        if (battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        {
            //use damage protection move : protect
            var zigzagoonParticipant = battleHandler.GetParticipant(BattleParticipantKey.Player);
            var treeckoParticipant = battleHandler.GetParticipant(BattleParticipantKey.Enemy);

            var tackle = treeckoParticipant.pokemon.moveSet[0];
            tackle.isSureHit = true;//make sure it doesnt miss
            
            var protect = zigzagoonParticipant.pokemon.moveSet[0];//protect never misses
            battleHandler.UseMove(protect,zigzagoonParticipant, BattleParticipantKey.Enemy);
        }
    }
}
