using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SemiInvulnerableSingleBattleTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private MoveSequenceHandler _moveUsageHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        
        testName = "Semi Invulnerability Single Battle Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //first fly
        _sequencer.AddAction(AttackFirst);
        _sequencer.AddAction(EnsureHitAndSkipTurn);
        _sequencer.AddAction(AllowEnemyToCounter);
        _sequencer.AddAction(EnsureHitAndSkipTurn);
        _sequencer.AddAction(HijackEnemyForFreeSwitch);
        //then dig
        _sequencer.AddAction(AttackFirst);
        _sequencer.AddAction(EnsureHitAndSkipTurn);
        _sequencer.AddAction(AllowEnemyToCounter);
        _sequencer.AddAction(EnsureHitAndSkipTurn);
        _moveUsageHandler.OnDamageModified += CheckForInvulnerableDamageEffect;
    }
   
    private void HijackEnemyForFreeSwitch()
    {
        _sequencer.RemoveLastAction();
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySkip);
        _pokemonPartyHandler.SwapToPartner();
        return;
        void ForceEnemySkip()
        {
            _turnBasedCombatHandler.SaveEmptyTurn(_battleHandler.GetCurrentParticipant().participantKey);
        }
    }
    private void AttackFirst()
    {
        //To make test case reliable
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        currentParticipant.pokemon.moveSet[0].priority = 100;
        _sequencer.UseMove();
    }
    private void AllowEnemyToCounter()
    {
        //give enemy a move that can negate 
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var semiData = player.pokemon.moveSet[0].GetModule<SemiInvulnerabilityInfo>().semiInvulnerabilities;
        var movesThatCounter = semiData.Select(data => data.moveName).ToList();
        
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        //enemy will use tackle but disguised as a counter viable move
        enemy.pokemon.moveSet[0].moveName = NameDB.GetMoveName(movesThatCounter[0]);
        enemy.pokemon.moveSet[0].isSureHit = true;
        testingHandler.LogMessage($"changed enemy move to {enemy.pokemon.moveSet[0].moveName}",TestLogType.Information);
        AttackFirst();
    }
    
    private void EnsureHitAndSkipTurn()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        //semi-invulnerable logic removes sure hit when it's about to
        //deal damage, so revert it for testing purposes
        player.semiInvulnerabilityData.turnData.move.isSureHit = true;
        player.semiInvulnerabilityData.turnData.move.priority = 100;
        _turnBasedCombatHandler.NextTurn();
    }
    void CheckForInvulnerableDamageEffect(DamageCalculationModifier modifier,float initialDamage,float modifiedDamage)
    {
        if (modifier == DamageCalculationModifier.SemiInvulnerable)
        {
            testingHandler.LogMessage("Semi-Invulnerability damage change is in effect",  TestLogType.Information);
            var increase = modifiedDamage > initialDamage;
            
            var message = increase
                ? $"vulnerability increased damage from {initialDamage} to {modifiedDamage}"
                : $"vulnerability reduced damage from {initialDamage} to {modifiedDamage}";
            testingHandler.LogMessage(message, TestLogType.Calculation);
        }
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
      
        _testCaseHandler.AddTestCase(0,"Player has full health",() => player.pokemon.hp >= player.pokemon.maxHp);
        _testCaseHandler.AddTestCase(2,"Player has to be damaged",() => player.pokemon.hp < player.pokemon.maxHp);
        _testCaseHandler.AddTestCase(5,"Player has full health",() => player.pokemon.hp >= player.pokemon.maxHp);
        _testCaseHandler.AddTestCase(7,"Player has to be damaged",() => player.pokemon.hp < player.pokemon.maxHp);

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
        
        var caseExists = _testCaseHandler.HandleCurrentTestCase(CheckTestEnd,TestCaseFailed);
        if (!caseExists)
        {
            CheckTestEnd();
        }
        void CheckTestEnd()
        {
            //make test more reliable,prevent conflicting outcomes
            enemy.pokemon.hp = enemy.pokemon.maxHp;
            player.pokemon.hp = player.pokemon.maxHp;
            if (_sequencer.SequenceComplete())
            {
                _moveUsageHandler.OnDamageModified -= CheckForInvulnerableDamageEffect;
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            _moveUsageHandler.OnDamageModified -= CheckForInvulnerableDamageEffect;
            EndTest(false);
        }
    }
    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey != BattleParticipantKey.Player) return;
        _sequencer.CallNextAction();
    }
}
