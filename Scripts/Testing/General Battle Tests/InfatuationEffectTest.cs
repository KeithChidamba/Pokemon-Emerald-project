using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class InfatuationEffectTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
     
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Infatuation Effect Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //should fail
        _sequencer.AddAction(()=>TestAttractWithGender(Gender.Female));
        //should fail
        _sequencer.AddAction(()=>TestAttractWithGender(Gender.None));
        //will work
        _sequencer.AddAction(()=>TestAttractWithGender(Gender.Male));
        
        //test the effect of infatuation and reject re-use of infatuation by player
        _sequencer.AddAction(AttackNormally);

        //swap to cancel infatuation
        _sequencer.AddAction(HijackEnemyForFreeSwitch);
        //should damage player
        _sequencer.AddAction(AttackNormally);
    }

    private void AttackNormally()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Natural);
        enemy.pokemon.moveSet[0].isSureHit = true;
        _sequencer.UseMove();//tackle
    }
    private void TestAttractWithGender(Gender gender)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        enemy.pokemon.gender = gender;
        player.pokemon.gender = Gender.Female;
        
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySkip);
        
        player.pokemon.moveSet[0].priority = 100;
        _sequencer.UseMove();//attract
    }
    private void ForceEnemySkip()
    {
        _turnBasedCombatHandler.SaveEmptyTurn(_battleHandler.GetCurrentParticipant().participantKey);
    }
    private void HijackEnemyForFreeSwitch()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySkip);
        _pokemonPartyHandler.SwapToPartner();
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase(0,"Enemy must not be infatuated",() => !enemy.isInfatuated);
        _testCaseHandler.AddTestCase(1,"Enemy must not be infatuated",() => !enemy.isInfatuated);
        _testCaseHandler.AddTestCase(2,"Enemy must be infatuated",() => enemy.isInfatuated);
        _testCaseHandler.AddTestCase(4,"Enemy must not be infatuated",() => !enemy.isInfatuated);
        _testCaseHandler.AddTestCase(5,"Player must be damaged",
            () => player.pokemon.hp < player.pokemon.maxHp);

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
        
        var caseExists = _testCaseHandler.CheckForCurrentTestCase(CheckTestEnd,TestCaseFailed);
        if (!caseExists)
        {
            CheckTestEnd();
        }
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

