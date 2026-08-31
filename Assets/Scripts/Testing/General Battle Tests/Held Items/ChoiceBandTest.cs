using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class ChoiceBandTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private MoveSequenceHandler _moveUsageHandler;

    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;

    private Dictionary<BattleParticipantKey, MoveName> _moveUsageHistory = new();
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Choice Band Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //Use move tackle
        _sequencer.AddAction(ActivateChoiceBandLockForPlayer);
        _sequencer.AddAction(ImitatePlayerChoosingToAttack);
        //swap to remove choice band effect, and remove heldItem
        _sequencer.AddAction(() =>
        {
            HijackEnemyForFreeSwitch();
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            player.pokemon.RemoveHeldItem();
        });
        _sequencer.AddAction(HijackEnemyForFreeSwitch);
        
        //enemy use choice band
        _sequencer.AddAction(SetupEnemyChoiceBand);
        _sequencer.AddAction(AttackNormally);
        
        _moveUsageHandler.OnMoveHit += StoreMovesUsed;
    }

    private void AttackNormally()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Natural);
        _sequencer.UseMove();
    }
    private void StoreMovesUsed(BattleParticipant attacker,BattleParticipant victim,Move moveUsed,float finalDamage)
    {
        _moveUsageHistory.Add(attacker.participantKey,NameDB.ParseMoveName(moveUsed.moveName));
    }
    private void SetupEnemyChoiceBand()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var choiceBand = Resources.Load<Item>(DirectoryHandler.GetDirectory(AssetDirectory.Items)+"Choice Band");
        if (choiceBand == null)
        {
            Debug.LogError($"Choice band not found at {DirectoryHandler.GetDirectory(AssetDirectory.Items)+"Choice Band"}");
            EndTest(false);
        }
        enemy.pokemon.GiveItem(InstanceFactory.CreateItem(choiceBand));
        enemy.heldItemHandler.SetHeldItemEffect();
        
        enemy.pokemonTrainerAI.AssignBehaviorAction(()=>
        {
            //for enemy to sure hit tackle to trigger choice band
            enemy.pokemon.moveSet[0].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet[0], enemy, BattleParticipantKey.Player);
        });
        _sequencer.UseMove();
    }
    private void HijackEnemyForFreeSwitch()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(()=>
        {
            _turnBasedCombatHandler.SaveEmptyTurn(_battleHandler.GetCurrentParticipant().participantKey);
        });
        _pokemonPartyHandler.SwapToPartner();
    }
    private void ImitatePlayerChoosingToAttack()
    {  
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        player.pokemon.moveSet[0].isSureHit = true;
        _battleHandler.LoadMoveInputAndText();
    }
    private void ActivateChoiceBandLockForPlayer()
    {
        //choice band activate after first successful move
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        player.pokemon.moveSet[0].priority = 100;
        _sequencer.UseMove();
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
           new("Check if player used tackle",()=>_moveUsageHistory[BattleParticipantKey.Player] == MoveName.Tackle),
           new("Choice band locked move",()=>player.currentMoveLock.moveLocked),
           new("Check if the locked move is tackle",()=>player.currentMoveLock.moveToLock.moveName == NameDB.GetMoveName(MoveName.Tackle)),
           new("check if player got attack buff from choice band",()=>player.pokemon.attack > player.statData.attack)
        });
        
        _testCaseHandler.AddTestCase("Check Player used tackle because of lock",
            () => _moveUsageHistory[BattleParticipantKey.Player] == MoveName.Tackle
                  && player.currentMoveLock.moveLocked);
        
        _testCaseHandler.AddTestCase("Choice band effect should be removed after switch",
            () => !player.currentMoveLock.moveLocked);
        
        _testCaseHandler.AddTestCase("Choice band attack buff should be removed after switch",
            () => player.pokemon.attack <= player.statData.attack);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Enemy should have a choice band",
                ()=> enemy.pokemon.hasItem 
                     && enemy.pokemon.heldItem.itemName == "Choice Band"),
            
            new("Choice band locked move", ()=> enemy.currentMoveLock.moveLocked),
            
            new("Locked move is tackle",
                ()=> enemy.currentMoveLock.moveToLock.moveName == NameDB.GetMoveName(MoveName.Tackle)),
            
            new("check if enemy got attack buff from choice band",
                ()=> enemy.pokemon.attack > enemy.statData.attack)
        });
               
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Choice band locked move", ()=> enemy.currentMoveLock.moveLocked),
            new("Check Enemy used tackle because of lock",
                ()=> _moveUsageHistory[BattleParticipantKey.Enemy] == MoveName.Tackle),
        });
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);

        var caseExists = _testCaseHandler.CheckForCurrentTestCase(CheckTestEnd,TestCaseFailed);
        if (!caseExists)
        {
            CheckTestEnd();
        }
        return;
        void CheckTestEnd()
        {
            enemy.pokemon.hp = enemy.pokemon.maxHp;
            player.pokemon.hp = player.pokemon.maxHp;
            _moveUsageHistory.Clear();
            if (_sequencer.SequenceComplete())
            {
                _moveUsageHandler.OnMoveHit -= StoreMovesUsed;
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            _moveUsageHandler.OnMoveHit -= StoreMovesUsed;
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

