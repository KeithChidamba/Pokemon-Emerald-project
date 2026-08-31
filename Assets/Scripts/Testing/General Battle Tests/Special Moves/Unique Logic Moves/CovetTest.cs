using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class CovetTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    private string _berryName;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Covet Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        _berryName = "Oran berry";
        
        //Covet, should fail if enemy has no item
        _sequencer.AddAction(()=>_sequencer.UseMove());
        //Covet, should fail because enemy Item is not a berry
        _sequencer.AddAction(()=>AttackAndTakePotion("Potion"));
        //Covet, should take enemy berry
        _sequencer.AddAction(()=>AttackAndTakePotion(_berryName));
    }
    private void AttackAndTakePotion(string itemName)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var assetDirectory = DirectoryHandler.GetDirectory(AssetDirectory.Items) + itemName;
        var oranBerry = Resources.Load<Item>(assetDirectory);
        if (oranBerry == null)
        {
            Debug.LogError($"item not found: {assetDirectory}");
            EndTest(false);
        }
        enemy.pokemon.GiveItem(oranBerry);
        
        _sequencer.UseMove();
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase("Covet should not take item from enemy, but deal damage",
            () => !player.pokemon.hasItem
            && enemy.pokemon.hp < enemy.pokemon.maxHp);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Covet should not take item from enemy",()=> !player.pokemon.hasItem),
            new("Covet should hurt enemy",()=> enemy.pokemon.hp<enemy.pokemon.maxHp),
        });
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Covet should not take item from enemy because the item is not a berry",()=> !player.pokemon.hasItem),
            new("Enemy should still have item",()=> enemy.pokemon.hasItem),
        });
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new($"Covet should take {_berryName} from enemy",()=>  player.pokemon.hasItem),
            new("Enemy should not have item",()=> !enemy.pokemon.hasItem),
            new($"Player's item should be the specified berry {_berryName}",
                () => player.pokemon.heldItem.itemName==_berryName)
        });
        
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

