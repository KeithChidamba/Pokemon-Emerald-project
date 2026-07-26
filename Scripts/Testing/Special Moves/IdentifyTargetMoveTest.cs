using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdentifyTargetMoveTest : BattleMoveUsageTest
{
    private TestActionSequencer _sequencer;
    
    private Battle_handler _battleHandler;
    private Pokemon_party _pokemonPartyHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<Battle_handler>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _sequencer = new TestActionSequencer(1);
        testName = "Identify Target Test";
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(() => UseMove(1),false);//brick break,should fail because of ghost type
        _sequencer.AddAction(() => UseMove(0),false);//odor-sleuth
        _sequencer.AddAction(() => UseMove(1),true);//brick break,should hit
        _sequencer.AddAction(() => UseMove(0),false);//test odor sleuth move repeat, should fail
        _sequencer.AddAction(SwitchToPartner,false);
    }
    
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
    private void CheckMoveEffectRemoval(Battle_Participant swapParticipant)
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
            SetStatus(true);
            EndTest();
        }
    }
    
    private void SwitchToPartner()
    {
        //swap with your partner
        _battleHandler.OnSwitchOut += CheckMoveEffectRemoval;
        _pokemonPartyHandler.BeginMemberSwap(1);
        //repeat everything but with tackle and foresight
        _sequencer.RepeatSequence();
    }
    private void UseMove(int currentMoveUsageIndex)
    {
        var playerParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        //use immunity negation move : odor-sleuth or foresight
        var move = playerParticipant.pokemon.moveSet[currentMoveUsageIndex];
        move.isSureHit = true;
        _battleHandler.UseMove(move,playerParticipant, BattleParticipantKey.Enemy);
    }

    private void LogImmunityState(Battle_Participant enemy)
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

            testingHandler.LogMessage($"Victim has buff count: {enemy.pokemon.buffAndDebuffs.Count}",
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
            enemy.pokemon.buffAndDebuffs.Add(new Buff_Debuff(Stat.Evasion,1));
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
                SetStatus(false);
                EndTest();
                return;
            }
        }
        _sequencer.CallNextAction();
    }
}
