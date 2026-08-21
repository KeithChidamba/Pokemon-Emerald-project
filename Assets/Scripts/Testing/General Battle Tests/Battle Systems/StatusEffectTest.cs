using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class StatusEffectTest : BattleBasedTest
{
    private BattleHandler _battleHandler;

    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    private MoveName[] _statusMoves =
    {
        MoveName.Ember,
        MoveName.IceBeam,
        MoveName.Flamethrower,
        MoveName.SleepPowder,
        MoveName.PoisonSting,
        MoveName.ThunderWave,
        MoveName.Toxic,
        //Turn placeholder for toxic test cases
        MoveName.TailWhip,
        MoveName.ConfuseRay,
        //Turn placeholder for confusion test cases
        MoveName.TailWhip
    };
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Status Effect Test";

        testExitCondition = TestCompletionCondition.EndManually;
        
        //use status effects
        for (int i = 0; i < _statusMoves.Length; i++)
        {
            _sequencer.AddAction(UseStatusMove);
        }
    }
    private void UseStatusMove()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        
        var moveName = NameDB.GetMoveName(_statusMoves[_sequencer.GetTestCaseIndex()]);
        
        var assetPath = DirectoryHandler.GetDirectory(AssetDirectory.Moves) + moveName;
        var moveFromAsset = Resources.Load<Move>(assetPath);
        var newMove = InstanceFactory.CreateMove(moveFromAsset);
        newMove.priority = 100;
        
        /*give flamethrower 5 damage to comply with the move pipeline
        and allow it to remove freeze effect, while not fainting the enemy*/
        if (_statusMoves[_sequencer.GetTestCaseIndex()] != MoveName.Flamethrower)
        {
            newMove.moveDamage = 0;
            newMove.statusChance = 100;
        }
        else
        {
            newMove.moveDamage = 5;
            newMove.statusChance = 0;
        }
        currentParticipant.pokemon.moveSet[0] = newMove;
        _sequencer.UseMove();
    }

    public override IEnumerator BeginTest()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.statusHandler.ChangeToTestingState(StatusHandlingState.Permanent);
        
        _testCaseHandler.AddTestCase( "Victim has to suffer burn", 
            () => enemy.pokemon.hp < enemy.pokemon.maxHp
                  && enemy.pokemon.attack < enemy.statData.attack
                  && enemy.pokemon.statusEffect == StatusEffect.Burn);
        
        _testCaseHandler.AddTestCase("Victim has to be frozen", 
            () => !enemy.canAttack
                 && enemy.pokemon.statusEffect == StatusEffect.Freeze);
        
        _testCaseHandler.AddTestCase( "Victim has to be unfrozen", 
            () => enemy.canAttack
                  && enemy.pokemon.statusEffect == StatusEffect.None);
        
        _testCaseHandler.AddTestCase( "Victim has to be asleep", 
            () => !enemy.canAttack
            && enemy.pokemon.statusEffect == StatusEffect.Sleep);
        
        _testCaseHandler.AddTestCase( "Victim has to be poisoned", 
            () => enemy.pokemon.hp < enemy.pokemon.maxHp
            &&  enemy.pokemon.statusEffect == StatusEffect.Poison);
        
        _testCaseHandler.AddTestCase( "Victim has to be paralyzed", 
            () => enemy.pokemon.speed < enemy.statData.speed
            &&  enemy.pokemon.statusEffect == StatusEffect.Paralysis);
        
        _testCaseHandler.AddTestCase("Victim has to be poisoned badly", 
            () => enemy.pokemon.hp < enemy.pokemon.maxHp
                  && enemy.pokemon.statusEffect == StatusEffect.BadlyPoison);
        
        _testCaseHandler.AddTestCase( "Victim's poisoned status should be reduced after it switches out", 
            () => enemy.pokemonTrainerAI.TrainerParty[1].hp < enemy.pokemonTrainerAI.TrainerParty[1].maxHp
                  && enemy.pokemonTrainerAI.TrainerParty[1].statusEffect == StatusEffect.Poison);
        
        _testCaseHandler.AddTestCase( "Victim should be in confusion", 
            () => enemy.isConfused);
        
        _testCaseHandler.AddTestCase( "Victim should be healed from confusion", 
            () => !enemy.isConfused);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        testingHandler.LogMessage($"Health of enemy: {enemy.pokemon.hp}" +
                                  $"/{enemy.pokemon.maxHp}",TestLogType.Health);
        testingHandler.LogMessage($"Status of enemy: {enemy.pokemon.statusEffect}" +
                                  $"/ Status of enemy partner:{enemy.pokemonTrainerAI.TrainerParty[1].statusEffect}"
            ,TestLogType.Information);

         _testCaseHandler.HandleCurrentTestCase(CheckTestEnd, TestCaseFailed);
        return;
        void CheckTestEnd()
        {
            if (enemy.pokemon.statusEffect==StatusEffect.BadlyPoison)
            {
                enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
                enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySwap);
                void ForceEnemySwap()
                {
                    //swap to partner to test poison change
                    enemy.pokemonTrainerAI.SwitchPokemon(1);
                }
            } 
            //freeze has a test case that requires it's status to remain
            else if (enemy.pokemon.statusEffect != StatusEffect.Freeze)
            {
                enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Natural);
                enemy.pokemon.hp = enemy.pokemon.maxHp;
                enemy.statusHandler.RemoveStatusEffect(true);
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