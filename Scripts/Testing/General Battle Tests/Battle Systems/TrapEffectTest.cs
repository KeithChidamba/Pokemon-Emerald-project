using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class TrapEffectTest : BattleBasedTest
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
        
        testName = "Trap Effect Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        //tackle, and mean look
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
        //tailwhip, sand tomb
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(1,1));
        //setup fast trap removal
        _sequencer.AddAction(HijackEnemyTurnAndSetupSandTomb);
        //enemy switch out to free player
        _sequencer.AddAction(ForceEnemySwitch);
    }

    private void ForceEnemySwitch()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySwap);
        _turnBasedCombatHandler.SaveEmptyTurn(BattleParticipantKey.Player);
        return;
        void ForceEnemySwap()
        {
            enemy.pokemonTrainerAI.SwitchPokemon(1);
        }
    }
    private void ForceEnemyMove(int moveIndex)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseSpecificMove);
        return;
        void UseSpecificMove()
        {
            enemy.pokemon.moveSet[moveIndex].priority = 100;
            enemy.pokemon.moveSet[moveIndex].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet[moveIndex], enemy, BattleParticipantKey.Player);
        }
    }

    private void HijackEnemyTurnAndSetupSandTomb()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player); 
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySkip);
        
        var copyOfSandTomb = player.statusHandler.CurrentTraps[2];
        copyOfSandTomb.trapDuration = 0;
        player.statusHandler.SetupTrapDuration(copyOfSandTomb, false);
        _sequencer.UseMove();
        
        return;
        void ForceEnemySkip()
        {
            _turnBasedCombatHandler.SaveEmptyTurn(BattleParticipantKey.Enemy);
        }
    }
    private bool PlayerSwitchIsPrevented()
    {
        return !_pokemonPartyHandler.IsValidSwap(1, true,false);
    }
    private void ForceEnemyMoveAndAttack(int moveIndex,int enemyMoveIndex)
    {
        ForceEnemyMove(enemyMoveIndex);
        _sequencer.UseMove(moveIndex);
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player); 
        
        _testCaseHandler.AddTestCase("Arena trap And Mean look",
            () => player.statusHandler.CurrentTraps[0].trapType
                  == TrapData.TrapType.PersistentFromAbility
                  && player.statusHandler.CurrentTraps[1].trapType
                  == TrapData.TrapType.PersistentFromMove 
                  && PlayerSwitchIsPrevented());
        
        _testCaseHandler.AddTestCase("Player can't switch due to sand Tomb",
            () => player.statusHandler.CurrentTraps[2].trapType 
                  == TrapData.TrapType.RandomDurationFromMove
                  && PlayerSwitchIsPrevented());
        
        _testCaseHandler.AddTestCase("Sand tomb should be gone",
            () => 
                player.statusHandler.CurrentTraps.Count == 2
                && !player.canEscape);
        
        _testCaseHandler.AddTestCase("Player should be freed",
            () => 
                player.statusHandler.CurrentTraps.Count == 0 
                && player.canEscape);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        var caseExists = _testCaseHandler.CheckForCurrentTestCase(CheckTestEnd,TestCaseFailed);
        if (!caseExists)
        {
            CheckTestEnd();
        }
        return;
        void CheckTestEnd()
        {
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player); 
            var currentTraps = player.statusHandler.CurrentTraps;
            
            testingHandler.LogMessage($"Number of traps on player: {currentTraps.Count}", TestLogType.Information);
            foreach (var trap in currentTraps)
            {
                testingHandler.LogMessage($"Trap type: {trap.trapType}", TestLogType.Information);
            }
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

