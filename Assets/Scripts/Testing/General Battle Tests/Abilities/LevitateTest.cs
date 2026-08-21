using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class LevitateTest : BattleBasedTest
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
        testName = "Levitate Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        //player use tackle, enemy use mud slap
        
        _sequencer.AddAction(AttackNormally);
        _sequencer.AddAction(SetupAbilityChange);
    }
    private void AttackNormally()
    { 
        _sequencer.UseMove();//tackle
        
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseMove);
        return;
        void UseMove()
        {
            //mud slap
            enemy.pokemon.moveSet[0].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet[0], enemy, BattleParticipantKey.Player);
        }
    }
    private void SetupAbilityChange()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var levitateAsset = Resources.Load<Ability>(DirectoryHandler.GetDirectory(AssetDirectory.Abilities) + nameof(AbilityName.Levitate));
        player.pokemon.ability = levitateAsset;
        player.abilityHandler.ResetState();
        player.abilityHandler.SetAbilityMethod();
        _sequencer.UseMove();
        testingHandler.LogMessage($"player participant artificially received ability {player.pokemon.ability.abilityName}" +
                                  $", which grants additional immunity to {player.additionalTypeImmunity.typeEnum}",TestLogType.Information);
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        _testCaseHandler.AddTestCase("Player should be damaged by ground move",
            () => player.pokemon.hp < player.pokemon.maxHp);
        
        _testCaseHandler.AddTestCase("Player should be immune to ground move, and have levitate",
            () => player.pokemon.hp >= player.pokemon.maxHp
            && player.pokemon.ability.abilityName == AbilityName.Levitate
            && player.additionalTypeImmunity.typeEnum == PokemonType.Ground);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
       var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
       testingHandler.LogMessage($"Health of player: {player.pokemon.hp}" +
                                  $"/{player.pokemon.maxHp}",TestLogType.Health);

        _testCaseHandler.HandleCurrentTestCase(CheckTestEnd,TestCaseFailed);
        return;
        void CheckTestEnd()
        {
            //for test cases
            player.pokemon.hp = player.pokemon.maxHp;
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

