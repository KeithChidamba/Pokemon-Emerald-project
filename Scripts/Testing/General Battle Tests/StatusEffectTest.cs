using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class StatusEffectTest : BattleBasedTest
{
    private BattleHandler _battleHandler;

    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;

    private int _currentMoveIndex;
    private LearnSetMoveName[] _statusMoves =
    {
        LearnSetMoveName.Ember,
        LearnSetMoveName.IceBeam,
        LearnSetMoveName.Flamethrower,
        LearnSetMoveName.SleepPowder,
        LearnSetMoveName.PoisonSting,
        LearnSetMoveName.ThunderWave,
        LearnSetMoveName.Toxic,
        LearnSetMoveName.TailWhip,
        LearnSetMoveName.TailWhip
    };
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        
        _sequencer = new MoveTestActionSequencer(container,7);
        _testCaseHandler = new TestCaseHandler(testingHandler);
        testName = "Status Effect Test";

        testExitCondition = TestCompletionCondition.EndManually;
        
        //use status effects
        _sequencer.AddAction(UseStatusMove);
    }
    private void UseStatusMove()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        
        var moveName = NameDB.GetMoveName(_statusMoves[_currentMoveIndex]);
        
        var assetPath = DirectoryHandler.GetDirectory(AssetDirectory.Moves) + moveName;
        var moveFromAsset = Resources.Load<Move>(assetPath);
        var newMove = InstanceFactory.CreateMove(moveFromAsset);
        newMove.priority = 100;
        
        /*give flamethrower 5 damage to comply with move pipeline
        and allow it to remove freeze effect, while not fainting enemy*/
        newMove.moveDamage = _statusMoves[_currentMoveIndex] != LearnSetMoveName.Flamethrower? 
                0 : 5;
        
        newMove.statusChance = 100;
        currentParticipant.pokemon.moveSet[0] = newMove;
        
        _sequencer.UseMove();
    }

    public override IEnumerator BeginTest()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.statusHandler.ChangeToTestingState(StatusHandlingState.Permanent);
        
        _testCaseHandler.AddTestCase(0, "Victim has to suffer burn", 
            () => enemy.pokemon.hp < enemy.pokemon.maxHp
                  && enemy.pokemon.attack < enemy.statData.attack
                  && enemy.pokemon.statusEffect == StatusEffect.Burn);
        
        _testCaseHandler.AddTestCase(1, "Victim has to be frozen", 
            () => !enemy.canAttack
                 && enemy.pokemon.statusEffect == StatusEffect.Freeze);
        
        _testCaseHandler.AddTestCase(2, "Victim has to be unfrozen", 
            () => enemy.canAttack
                  && enemy.pokemon.statusEffect == StatusEffect.None);
        
        _testCaseHandler.AddTestCase(3, "Victim has to be asleep", 
            () => !enemy.canAttack
            && enemy.pokemon.statusEffect == StatusEffect.Sleep);
        
        _testCaseHandler.AddTestCase(4, "Victim has to be poisoned", 
            () => enemy.pokemon.hp < enemy.pokemon.maxHp
            &&  enemy.pokemon.statusEffect == StatusEffect.Poison);
        
        _testCaseHandler.AddTestCase(5, "Victim has to be paralyzed", 
            () => enemy.pokemon.speed < enemy.statData.speed
            &&  enemy.pokemon.statusEffect == StatusEffect.Paralysis);
        
        _testCaseHandler.AddTestCase(6, "Victim has to be poisoned badly", 
            () => enemy.pokemon.hp < enemy.pokemon.maxHp
                  && enemy.pokemon.statusEffect == StatusEffect.BadlyPoison);
        
        _testCaseHandler.AddTestCase(7, "Victim's poisoned status should be reduced after switching out", 
            () => enemy.pokemonTrainerAI.trainerParty[1].hp < enemy.pokemonTrainerAI.trainerParty[1].maxHp
                  && enemy.pokemonTrainerAI.trainerParty[1].statusEffect == StatusEffect.Poison);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        var caseExists = _testCaseHandler
            .HandleCurrentTestCase(_currentMoveIndex
                , CheckTestEnd, TestCaseFailed);
        
        _currentMoveIndex++;
        return;
        void CheckTestEnd()
        {
            if (enemy.pokemon.statusEffect==StatusEffect.BadlyPoison)
            {
                enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
                enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySwap);
                void ForceEnemySwap()
                {
                    //swap to partner to test poison change
                    enemy.pokemonTrainerAI.SwitchPokemon(1);
                }
            }
            else if (enemy.pokemon.statusEffect != StatusEffect.Freeze)
            {
                enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Natural);
                enemy.pokemon.hp = enemy.pokemon.maxHp;
                enemy.statusHandler.RemoveStatusEffect();
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