using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class StatusEffectTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
    
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

    private bool _damageDecreased;
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        
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
        var moveEnum = _statusMoves[_sequencer.GetTestCaseIndex()];
        var moveName = NameDB.GetMoveName(moveEnum);
        
        var assetPath = DirectoryHandler.GetDirectory(AssetDirectory.Moves) + moveName;
        var moveFromAsset = Resources.Load<Move>(assetPath);
        var newMove = InstanceFactory.CreateMove(moveFromAsset);
        newMove.priority = 100;
        
        /*give flamethrower 5 damage to comply with the move pipeline
        and allow it to remove freeze effect, while not fainting the enemy*/
        if (moveEnum == MoveName.Flamethrower)
        {
            newMove.moveDamage = 5;
            newMove.statusChance = 0;
        }
        else
        {
            newMove.moveDamage = 0;
            newMove.statusChance = 100;
        }
        currentParticipant.pokemon.moveSet[0] = newMove;
        
        if (moveEnum == MoveName.Ember)
        {
            SetupEnemyAttackForBurnEffect();
        }
        _sequencer.UseMove();
    }

    private void SetupEnemyAttackForBurnEffect()
    {
        //burn status effect, make enemy use physical move to test damage reduction
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var tacklePath = DirectoryHandler.GetDirectory(AssetDirectory.Moves) + "Tackle";
        var tackleAsset = Resources.Load<Move>(tacklePath);
        var tackle = InstanceFactory.CreateMove(tackleAsset);
        enemy.pokemon.moveSet.Add(tackle);
            
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(() =>
        {
            enemy.pokemon.moveSet[0].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet.Last(), enemy, BattleParticipantKey.Player);
        });
        _moveUsageHandler.OnDamageModified += CheckForBurnEffect;
    }
    private void CheckForBurnEffect(DamageCalculationModifier modifier,float initialDamage,float modifiedDamage)
    {
        if (modifier == DamageCalculationModifier.StatusEffect)
        {
            var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
            enemy.pokemon.moveSet.Remove(enemy.pokemon.moveSet.Last());//remove added move
            _moveUsageHandler.OnDamageModified -= CheckForBurnEffect;
            _damageDecreased = modifiedDamage < initialDamage;
            var message = _damageDecreased ? "reduced":"increased";
            testingHandler.LogMessage( $"burn {message} the damage of the enemy from {initialDamage} to {modifiedDamage}",  TestLogType.Calculation);
        }
    }
    public override IEnumerator BeginTest()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.statusHandler.ChangeToTestingState();
        
        _testCaseHandler.AddTestCase( "Victim has to suffer burn and deal reduced physical damage because of burn", 
            () => enemy.pokemon.hp < enemy.pokemon.maxHp
                  && enemy.pokemon.statusEffect == StatusEffect.Burn
                  && _damageDecreased);
        
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
        
        _testCaseHandler.AddTestCase( "Victim's [Badly Poisoned] status should be reduced to [Poisoned] after it switches out", 
            () => enemy.pokemonTrainerAI.TrainerParty[1].hp < enemy.pokemonTrainerAI.TrainerParty[1].maxHp
                  && enemy.pokemonTrainerAI.TrainerParty[1].statusEffect == StatusEffect.Poison);
        
        _testCaseHandler.AddTestCase( "Victim should be in confusion and take damage", 
            () => enemy.isConfused && enemy.pokemon.hp < enemy.pokemon.maxHp);
        
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
        
         _testCaseHandler.HandleCurrentTestCase(CheckTestEnd, TestCaseFailed);
        return;
        void CheckTestEnd()
        {
            if (_sequencer.GetTestCaseIndex() == 7)
            {//Display the reduction of badly poison effect for test case 7
                testingHandler.LogMessage($"Status of enemy partner (who was switched out) : {enemy.pokemonTrainerAI.TrainerParty[1].statusEffect}"
                    ,TestLogType.Information);
            }
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
            //freeze has a test case that requires its status to remain
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