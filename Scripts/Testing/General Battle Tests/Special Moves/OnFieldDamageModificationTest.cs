using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OnFieldDamageModificationTest : BattleBasedTest
{
    private MoveSequenceHandler _moveUsageHandler;
    private BattleHandler _battleHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
   
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    private Dictionary<int,bool> damageModifications = new ();
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        
        testName = "On Field Damage Modification Test";
        testExitCondition = TestCompletionCondition.EndManually;
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        
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
        for (int i = 0; i < 7; i++)
        {
            var index = i;
            if (index == 1 || index == 2)
            {
                //skip turns that dont concern damage change
                //from field mods
                continue;
            }
            _testCaseHandler.AddTestCase(index,"field must modify Damage", () => damageModifications[index]);
        }
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
            damageModifications.Add(_sequencer.CurrentSequenceIndex-1,damageWasChanged);
            
            var increase = modifiedDamage > initialDamage;
            
            var message = increase
                ? $"field mod increased damage from {initialDamage} to {modifiedDamage}"
                : $"field mod reduced damage from {initialDamage} to {modifiedDamage}";
            
            testingHandler.LogMessage( message,  TestLogType.Calculation);
        }
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
            if (_sequencer.SequenceComplete())
            {
                _moveUsageHandler.OnDamageModified -= CheckForFieldEffect;
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            _moveUsageHandler.OnDamageModified -= CheckForFieldEffect;
            EndTest(false);
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
