using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OnFieldDamageModificationTest : BattleMoveUsageTest
{
    private MoveTestActionSequencer _sequencer;
    private Move_handler _moveUsageHandler;
    private Battle_handler _battleHandler;
    private Pokemon_party _pokemonPartyHandler;
    
    private List<bool> _damageChecks = new();
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _moveUsageHandler = container.Resolve<Move_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        
        testName = "On Field Damage Modification Test";
        testExitCondition = TestCompletionCondition.EndManually;
        _sequencer = new MoveTestActionSequencer(container);

        //Mud sport while enemy uses thunderbolt
        _sequencer.AddAction(() => _sequencer.UseMove());
        //swap to clear mud sport
        _sequencer.AddAction(_pokemonPartyHandler.SwapToPartner);
        //give enemy flamethrower
        _sequencer.AddAction(SetEnemyMoveAndAttack);
        //tail whip used as turn buffer to test rain effect
        _sequencer.AddAction(() => _sequencer.UseMove(1));
        //Water sport during rain
        _sequencer.AddAction(() => _sequencer.UseMove(2));
        //Sunny day during water sport
        _sequencer.AddAction(() => _sequencer.UseMove(3));
        //test solo damage increase from sunny day
        _sequencer.AddAction(RemoveWaterSport);
        
        _moveUsageHandler.OnDamageModified += CheckForFieldEffect;
    }
   
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    private void RemoveWaterSport()
    {
        testingHandler.LogMessage("Manually removed water sport",  TestLogType.Information);
        _moveUsageHandler.RemoveFieldDamageModifier(DamageModifierSource.WaterSport);
        //tail whip used as turn buffer
        _sequencer.UseMove(1);
    }
    private void SetEnemyMoveAndAttack()
    {
        var moveName = NameDB.GetMoveName(LearnSetMoveName.Flamethrower);
        var assetPath = DirectoryHandler.GetDirectory(AssetDirectory.Moves) + moveName;
        var moveFromAsset = Resources.Load<Move>(assetPath);
        var newMove = InstanceFactory.CreateMove(moveFromAsset);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemon.moveSet.RemoveAt(0);
        enemy.pokemon.moveSet.Add(newMove);
        //prevent burn to keep accurate damage values for test success 
        newMove.hasStatus = false;
        _sequencer.UseMove();//Rain dance
    }
    void CheckForFieldEffect(DamageCalculationModifier modifier,float initialDamage,float modifiedDamage)
    {
        if (modifier == DamageCalculationModifier.FieldModifiers)
        {
            var damageWasChanged = modifiedDamage < initialDamage || modifiedDamage > initialDamage;
            
            testingHandler.LogMessage(damageWasChanged? "Damage was changed":"Damage remained the same",  TestLogType.Information);
            _damageChecks.Add(damageWasChanged);
            
            var increase = modifiedDamage > initialDamage;
            
            var message = increase
                ? $"field mod increased damage from {initialDamage} to {modifiedDamage}"
                : $"field mod reduced damage from {initialDamage} to {modifiedDamage}";
            
            testingHandler.LogMessage( message,  TestLogType.Calculation);
        }
    }

    protected override void DetermineSuccess()
    {
        if (_sequencer.SequenceComplete())
        {
            _moveUsageHandler.OnDamageModified -= CheckForFieldEffect;
            var checksPassed = _damageChecks.All(check => check);
            SetStatus(checksPassed);
            EndTest();
        }
    }

    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey != BattleParticipantKey.Player) return;
        
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        //prevent paralysis from thunderbolt 
        enemy.pokemon.moveSet[0].hasStatus = false;
        enemy.pokemon.moveSet[0].isSureHit = true;
        
        testingHandler.LogMessage($"Health of player: {currentParticipant.pokemon.hp}" +
                                  $"/{currentParticipant.pokemon.maxHp}",TestLogType.Health);
        
        _sequencer.CallNextAction();
    }
    
}
