using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetAllExceptSelfTest : BattleMoveUsageTest
{
    private Battle_handler _battleHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<Battle_handler>();
        testName = "Target All Except Self Test";
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
        
        //for this test, the enemies are weak enough to faint after 1 hit so that becomes the test condition
        testingHandler.LogMessage($"Health of enemy target(Flying Type): {enemy.pokemon.hp}/{enemy.pokemon.maxHp}",TestLogType.Health);
        testingHandler.LogMessage($"Health of enemy partner target: {enemyPartner.pokemon.hp}/{enemyPartner.pokemon.maxHp}",TestLogType.Health);
        testingHandler.LogMessage($"Health of partner: {partnerParticipant.pokemon.hp}/{partnerParticipant.pokemon.maxHp}",TestLogType.Health);

        var testPassed = enemy.pokemon.hp >= enemy.pokemon.maxHp && 
                         enemyPartner.pokemon.hp <= 0 && 
                         partnerParticipant.pokemon.hp <= 0;
        
        SetStatus(testPassed);
    }

    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        
        if (currentParticipant.participantKey == BattleParticipantKey.Player)
        {
            //use multi target move : earthquake
            var marshtompParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            var earthquake = marshtompParticipant.pokemon.moveSet[0];
            earthquake.isSureHit = true;
            
            _battleHandler.UseMove(earthquake,marshtompParticipant,
                BattleParticipantKey.Enemy);//the target doesn't really matter here since the multi-target logic
                                            //is handled further down the pipeline
        }
        //this is just to progress the turn order, this never actually gets used
        if (currentParticipant.participantKey == BattleParticipantKey.PlayerPartner)
        {
            var partnerParticipant = _battleHandler.GetParticipant(BattleParticipantKey.PlayerPartner);
            var thunderBolt = partnerParticipant.pokemon.moveSet[0];
            _battleHandler.UseMove(thunderBolt,partnerParticipant,BattleParticipantKey.EnemyPartner);
        }
    }
}
