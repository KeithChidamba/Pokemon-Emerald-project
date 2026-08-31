using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class HealthBasedDamageBuffTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    private bool _damageWasChanged;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Health Based Damage Buff Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        //ember
        _sequencer.AddAction(()=> _sequencer.UseMove());
        //ember
        _sequencer.AddAction(AttackWithBuffedDamage);
        
        _moveUsageHandler.OnDamageModified += CheckForAbilityEffect;
    }
    private void AttackWithBuffedDamage()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        player.pokemon.hp = player.pokemon.maxHp * 0.25f;
        player.pokemon.moveSet[0].priority = 100;
        _sequencer.UseMove();
    }
    private void CheckForAbilityEffect(DamageCalculationModifier modifier,float initialDamage,float modifiedDamage)
    {
        if (modifier == DamageCalculationModifier.Ability)
        {
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            
            _damageWasChanged = modifiedDamage < initialDamage || modifiedDamage > initialDamage;
            if(_damageWasChanged)
            {
                var result = modifiedDamage > initialDamage? "increased":"decreased";
                testingHandler.LogMessage($"{player.pokemon.ability.abilityName} {result} the damage of " +
                                          $"{player.pokemon.moveSet[0].type.typeEnum}" +
                                          $" type move {player.pokemon.moveSet[0].moveName}",  TestLogType.Information);
                testingHandler.LogMessage($"{result} damage from {initialDamage} to {modifiedDamage}"
                    ,  TestLogType.Calculation);
            }
            
          
        }
    }
    public override IEnumerator BeginTest()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase(1,"ability should increase damage",
            () => _damageWasChanged && enemy.pokemon.hp < enemy.pokemon.maxHp);

        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        testingHandler.LogMessage($"Health of enemy: {enemy.pokemon.hp}" +
                                  $"/{enemy.pokemon.maxHp}",TestLogType.Health);
        testingHandler.LogMessage($"Health of player: {player.pokemon.hp}" +
                                  $"/{player.pokemon.maxHp}",TestLogType.Health);

        var caseExists= _testCaseHandler.CheckForCurrentTestCase(CheckTestEnd,TestCaseFailed);
        if (!caseExists)
        {
            CheckTestEnd();
        }
        return;
        void CheckTestEnd()
        {
            if (_sequencer.SequenceComplete())
            {
                _moveUsageHandler.OnDamageModified -= CheckForAbilityEffect;
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            _moveUsageHandler.OnDamageModified -= CheckForAbilityEffect;
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

