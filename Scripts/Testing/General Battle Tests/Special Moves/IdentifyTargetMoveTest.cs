using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdentifyTargetMoveTest : BattleBasedTest
{
    private MoveTestActionSequencer _sequencer;
    
    private BattleHandler _battleHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _sequencer = new MoveTestActionSequencer(container,1);
        testName = "Identify Target Test";
        testExitCondition = TestCompletionCondition.EndManually;
        //brick break,should fail because of ghost type
        _sequencer.AddAction(() => _sequencer.UseMove(1));
        //odor-sleuth
        _sequencer.AddAction(() => _sequencer.UseMove());
        //brick break,should hit
        _sequencer.AddAction(() => _sequencer.UseMove(1),true);
        //test odor sleuth move repeat, should fail
        _sequencer.AddAction(() => _sequencer.UseMove());
        
        _sequencer.AddAction(SwitchToPartner);
    }
    
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
    private void CheckMoveEffectRemoval(BattleParticipant swapParticipant)
    {
        if(swapParticipant.participantKey == BattleParticipantKey.Player)
        {
            _battleHandler.OnSwitchOut -= CheckMoveEffectRemoval;
            var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
            LogImmunityState(enemy);
        }
    }
    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        testingHandler.LogMessage($"Health of enemy: {enemy.pokemon.hp}" +
                                  $"/{enemy.pokemon.maxHp}",TestLogType.Health);
        
        if (_sequencer.SequenceComplete())
        {
            EndTest(true);
        }
    }
    
    private void SwitchToPartner()
    {
        //swap with your partner
        _battleHandler.OnSwitchOut += CheckMoveEffectRemoval;
        _pokemonPartyHandler.SwapToPartner();
    }


    private void LogImmunityState(BattleParticipant enemy)
    {
        if (enemy.immunityNegations.Count > 0)
        {
            testingHandler.LogMessage($"Victim has negation caused by: {enemy.immunityNegations[0].moveName}",
                TestLogType.Information);

            testingHandler.LogMessage(
                $"Victim is vulnerable to: {enemy.immunityNegations[0].ImmunityNegationTypes[0]} and " +
                $"{enemy.immunityNegations[0].ImmunityNegationTypes[1]}", TestLogType.Information);

            testingHandler.LogMessage($"Victim has an evasion of: {enemy.pokemon.evasion}",
                TestLogType.Information);

            testingHandler.LogMessage($"Victim has buff count: {enemy.pokemon.statModifiers.Count}",
                TestLogType.Information);
        }            
        else
        {
            testingHandler.LogMessage($"Victim has no negation ", TestLogType.Information);
        }
    }
    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey != BattleParticipantKey.Player) return;
        
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
       
        if (_sequencer.CurrentSequenceIndex == 0)
        {
            //first turn
            enemy.pokemon.types.Clear();
            var ghostType = Resources.Load<Type>(DirectoryHandler.GetDirectory(AssetDirectory.Types) + PokemonType.Ghost);
            enemy.pokemon.types.Add(ghostType);
            enemy.pokemon.statModifiers.Add(new StatChangeData(Stat.Evasion,1));
            enemy.pokemon.evasion = 133f;
        }
        if (_sequencer.CanDisplayCurrentLogs())
        { 
            LogImmunityState(enemy);
        }
        if (enemy.immunityNegations.Count > 0)
        {
            bool testSuccessful = true;
            if (enemy.immunityNegations[0].moveName == LearnSetMoveName.Foresight)
            {
                testSuccessful = enemy.pokemon.evasion <= 100;
            }
            if (!testSuccessful)
            {
                //fail if any section of test fails
                EndTest(false);
                return;
            }
        }
        _sequencer.CallNextAction();
    }
}
