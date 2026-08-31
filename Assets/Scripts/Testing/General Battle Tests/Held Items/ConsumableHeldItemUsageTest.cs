using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class ConsumableHeldItemUsageTest : BattleBasedTest
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
        testName = "Consumable Held Item Usage Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(SetupPlayerHealthAndAttack);
        _sequencer.AddAction(()=>SetupEnemyMoveAndBerry("Cherri berry",1));
        _sequencer.AddAction(()=>SetupEnemyMoveAndBerry("Persim berry",2));
    }
    
    private void ForceSpecificMove(int moveIndex=0)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemon.moveSet[moveIndex].isSureHit = true;
        enemy.pokemon.moveSet[moveIndex].priority = 100;
        _battleHandler.UseMove(enemy.pokemon.moveSet[moveIndex], enemy, BattleParticipantKey.Player);
    }
    private void SetupPlayerHealthAndAttack()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        //sonic boom
        enemy.pokemonTrainerAI.AssignBehaviorAction(()=>ForceSpecificMove());
        
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        //The enemy will use sonic boom to take health from 30 to 10
        player.pokemon.maxHp = 50f;
        player.pokemon.hp = 30;
        
        //tailwhip
        _sequencer.UseMove();
    }
    private void SetupEnemyMoveAndBerry(string berryName,int moveIndex)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.AssignBehaviorAction(()=>ForceSpecificMove(moveIndex));
        
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var assetDirectory = DirectoryHandler.GetDirectory(AssetDirectory.Items) + berryName;
        var persimBerry = InstanceFactory.CreateItem(Resources.Load<Item>(assetDirectory));
        player.pokemon.GiveItem(persimBerry);
        
        //tailwhip
        _sequencer.UseMove();
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        _testCaseHandler.AddTestCase("Oran berry must heal player from damage", 
            () => player.pokemon.hp > 10f);
        
        _testCaseHandler.AddTestCase("Cherri berry must heal player from paralysis", 
            () => player.pokemon.statusEffect == StatusEffect.None);
        
        _testCaseHandler.AddTestCase( "Player should be healed from confusion", 
            () => !player.isConfused);
        
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

        _testCaseHandler.HandleCurrentTestCase(CheckTestEnd,TestCaseFailed);
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