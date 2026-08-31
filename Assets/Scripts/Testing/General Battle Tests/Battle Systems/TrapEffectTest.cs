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
        
        //tailwhip, mean look
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
        //tailwhip, sand tomb
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,1));
        //setup fast trap removal
        _sequencer.AddAction(HijackEnemyTurnAndSetupSandTomb);
        //turn buffer [tailwhip and tailwhip]
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,2));
        //enemy switch out to free player
        _sequencer.AddAction(ForceEnemySwitch);
    }
    private void ForceEnemySwitch()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
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
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseSpecificMove);
        return;
        void UseSpecificMove()
        {
            //modified for test case reliability
            enemy.pokemon.moveSet[moveIndex].moveDamage = 0;
            enemy.pokemon.moveSet[moveIndex].priority = 100;
            enemy.pokemon.moveSet[moveIndex].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet[moveIndex], enemy, BattleParticipantKey.Player);
        }
    }

    private void HijackEnemyTurnAndSetupSandTomb()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player); 
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySkip);
        
        var copyOfSandTomb = player.statusHandler.CurrentTraps[2];
        copyOfSandTomb.trapDuration = 1;
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
        return !_pokemonPartyHandler.IsValidSwap(1,BattleParticipantKey.Player,true,false);
    }
    private void ForceEnemyMoveAndAttack(int moveIndex,int enemyMoveIndex)
    {
        ForceEnemyMove(enemyMoveIndex);
        _sequencer.UseMove(moveIndex);
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player); 
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Arena trap must be in effect",
                ()=> player.statusHandler.CurrentTraps[0].trapType
                     == TrapDataInfo.TrapType.PersistentFromAbility),
            new("Mean Look should be in effect",
                ()=> player.statusHandler.CurrentTraps[1].trapType
                     == TrapDataInfo.TrapType.PersistentFromMove),
            new("Player must be restricted from switching",PlayerSwitchIsPrevented),
        });
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Sand Tomb should be active",
                ()=> player.statusHandler.CurrentTraps.Last().trapType
                     == TrapDataInfo.TrapType.RandomDurationFromMove),
            new("player can't switch due to sand Tomb",PlayerSwitchIsPrevented),
        });
        
        _testCaseHandler.AddTestCase("Player should be damaged due to sand Tomb",
            () => player.pokemon.hp < player.pokemon.maxHp);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Sand tomb should be gone, so 2 traps remain",
                ()=> player.statusHandler.CurrentTraps.Count == 2),
            new("Player should still be restricted",()=> !player.canEscape),
        });
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("All traps should be gone",
                ()=> player.statusHandler.CurrentTraps.Count == 0),
            new("Player should be free",()=> player.canEscape),
        });
        
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

