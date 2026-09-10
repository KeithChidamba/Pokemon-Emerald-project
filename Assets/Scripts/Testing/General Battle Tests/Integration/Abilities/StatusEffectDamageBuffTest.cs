using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class StatusEffectDamageBuffTest : BattleBasedTest
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
        testName = "Status Effect Damage Buff Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        //This test will be done using [paralysis combo] ability
        
        //Thunder wave -> enemy use Tailwhip
        _sequencer.AddAction(()=>    
        {
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            player.pokemon.critChance = 0;
            _sequencer.UseMove();
        });
        //Tackle -> enemy use Tail whip
        _sequencer.AddAction(() => _sequencer.UseMove(1));
        
        _moveUsageHandler.OnDamageModified += CheckForAbilityEffect;
    }
  
    public override IEnumerator BeginTest()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase("enemy should be paralyzed",
            () => enemy.pokemon.statusEffect == StatusEffect.Paralysis);
        
        _testCaseHandler.AddTestCase("paralysis combo should increase damage", () => _damageWasChanged);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
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
                testingHandler.LogMessage($"{player.pokemon.ability.abilityName} {result} damage from {initialDamage} to {modifiedDamage}"
                    ,  TestLogType.Information);
            }
        }
    }
    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        testingHandler.LogMessage($"Health of enemy: {enemy.pokemon.hp}" +
                                  $"/{enemy.pokemon.maxHp}",TestLogType.Health);
        testingHandler.LogMessage($"Health of player: {player.pokemon.hp}" +
                                  $"/{player.pokemon.maxHp}",TestLogType.Health);

        _testCaseHandler.HandleCurrentTestCase(CheckTestEnd,TestCaseFailed);
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

