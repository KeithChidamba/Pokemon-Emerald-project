using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class  PursuitDoubleTargetTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;

    private long currentEnemyID;
    private int numAttacksOnEnemy;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
       
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Pursuit Double Target Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //Turn 1 -  test faint hit
        _sequencer.AddAction(() =>
        {
           ForceEnemySwitch(BattleParticipantKey.Enemy);//Enemy switch out, faints from pursuit damage
            currentEnemyID = _battleHandler.GetParticipant(BattleParticipantKey.Enemy).pokemon.pokemonID;
            
            //player use pursuit on enemy 
            _sequencer.UseMoveOnSpecific(
                0, BattleParticipantKey.Player,
                BattleParticipantKey.Enemy);
        });
        //partner use pursuit on enemy partner
        _sequencer.AddAction(() =>
        {
            _sequencer.UseMoveOnSpecific(
                0, BattleParticipantKey.PlayerPartner,
                BattleParticipantKey.EnemyPartner);
        });
        
        //Turn 2 - test double hit
        _sequencer.AddAction(() =>
        {
            ForceEnemySwitch(BattleParticipantKey.Enemy);//Enemy switch out success
            currentEnemyID = _battleHandler.GetParticipant(BattleParticipantKey.Enemy).pokemon.pokemonID;
             //player use pursuit on new enemy 
            _sequencer.UseMoveOnSpecific(
                0, BattleParticipantKey.Player,
                BattleParticipantKey.Enemy);
        });
        //partner use pursuit on new enemy 
        _sequencer.AddAction(() => _sequencer.UseMoveOnSpecific(
            0, BattleParticipantKey.PlayerPartner,
            BattleParticipantKey.Enemy));
        
        //Turn 3 -  test normal damage
        _sequencer.AddAction(() =>
        {
            //Allow enemies to attack normally
            var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
            enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Natural);
            //player use pursuit on enemy 
            _sequencer.UseMoveOnSpecific(
                0, BattleParticipantKey.Player,
                BattleParticipantKey.Enemy);
        });
        //partner use pursuit on enemy partner
        _sequencer.AddAction(() => _sequencer.UseMoveOnSpecific(
            0, BattleParticipantKey.PlayerPartner,
            BattleParticipantKey.EnemyPartner));
        
        _moveUsageHandler.OnMoveHit += TrackAttacks;
    }

    private void TrackAttacks(BattleParticipant attacker,BattleParticipant victim,Move moveUsed,float finalDamage)
    {
        if (victim.pokemon.pokemonID != currentEnemyID) return;
        numAttacksOnEnemy++;
    }
    private void ForceEnemySwitch(BattleParticipantKey enemyKey)
    {
        var enemy = _battleHandler.GetParticipant(enemyKey);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySwap);
        return;
        void ForceEnemySwap()
        {
            //skip participating pokemon
            var participatingIndex = _battleHandler.isDoubleBattle? 2:1;
            for (int i = participatingIndex; i < enemy.pokemonTrainerAI.TrainerParty.Count; i++)
            {
                if (enemy.pokemonTrainerAI.TrainerParty[i].hp <= 0) continue;
                enemy.pokemonTrainerAI.SwitchPokemon(i);
                break;
            }
        }
    }

    private Pokemon GetEnemyPokemonByID()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        return enemy.pokemonTrainerAI.TrainerParty.First(p => p.pokemonID == currentEnemyID);
    }
    public override IEnumerator BeginTest()
    {
        _testCaseHandler.AddTestCase("Pursuit must faint previous enemy on switch",
            () => GetEnemyPokemonByID().hp <= 0);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Both player's pursuit attacks must hit enemy on switch",
                () => numAttacksOnEnemy == 2),
            new("Enemy must not be fainted",
                () => GetEnemyPokemonByID().hp > 0)
        });
  
        //for testing purposes, disable the switch style
        _battleHandler.SetBattleStyle((int)BattleHandler.BattlesStyle.Set);
        
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
            //reset after each turn
            numAttacksOnEnemy = 0;
            currentEnemyID = 0;
            
            if (_sequencer.SequenceComplete())
            {
                _moveUsageHandler.OnMoveHit -= TrackAttacks;
                _battleHandler.SetBattleStyle((int)BattleHandler.BattlesStyle.Switch);
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            _moveUsageHandler.OnMoveHit -= TrackAttacks;
            _battleHandler.SetBattleStyle((int)BattleHandler.BattlesStyle.Switch);
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

