using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class ShedSkinTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
      
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Shed Skin Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(AttackNormally);
    }

    private void AttackNormally()
    { 
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseMove);
        _sequencer.UseMove();
        return;
        void UseMove()
        {
            //thunder wave
            enemy.pokemon.moveSet[0].priority = 100;
            enemy.pokemon.moveSet[0].statusChance = 100;
            enemy.pokemon.moveSet[0].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet[0], enemy, BattleParticipantKey.Player);
        }
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
       
        player.statusHandler.OnStatusCheck += HealStatusEffect;
        void HealStatusEffect()
        {
            //in-case the natural shed skin misses out due to random chance
            player.statusHandler.OnStatusCheck -= HealStatusEffect;
            player.abilityHandler.ShedSkinAbilityEffect(CommonRandom.Rnd100, player);
        }
        
        _testCaseHandler.AddTestCase("Player Shed skin must heal it",
            () => player.pokemon.statusEffect == StatusEffect.None);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        _testCaseHandler.CheckForCurrentTestCase(CheckTestEnd,TestCaseFailed);
        return;
        void CheckTestEnd()
        {
            if (_sequencer.SequenceComplete())
            {
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            //add extra logic here
            EndTest(false);
        }
    }
    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey is BattleParticipantKey.Enemy or BattleParticipantKey.EnemyPartner)
        {
            return;
        }
        _sequencer.CallNextAction();
    }
}

