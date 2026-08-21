using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdentifyTargetMoveTest : BattleBasedTest
{
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    private BattleHandler _battleHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        
        testName = "Identify Target Test";
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        
        //brick break,should fail because of ghost type
        _sequencer.AddAction(() => _sequencer.UseMove(1));
        //odor-sleuth
        _sequencer.AddAction(() => _sequencer.UseMove());
        //brick break,should hit
        _sequencer.AddAction(() => _sequencer.UseMove(1));
        //test odor sleuth move repeat, should fail
        _sequencer.AddAction(() => _sequencer.UseMove());
        
        _sequencer.AddAction(HijackEnemyForFreeSwitch);
        
        //tackle,should fail because of ghost type
        _sequencer.AddAction(() => _sequencer.UseMove(1));
        //Foresight
        _sequencer.AddAction(() => _sequencer.UseMove());
        //tackle,should hit
        _sequencer.AddAction(() => _sequencer.UseMove(1));
        //Foresight move repeat, should fail
        _sequencer.AddAction(() => _sequencer.UseMove());
    }
    private void HijackEnemyForFreeSwitch()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySkip);
        _battleHandler.OnSwitchOut += CheckMoveEffectRemoval;
        _pokemonPartyHandler.SwapToPartner();
        //test case index 5
        enemy.pokemon.hp = enemy.pokemon.maxHp;
        return;
        void ForceEnemySkip()
        {
            _turnBasedCombatHandler.SaveEmptyTurn(BattleParticipantKey.Enemy);
        }
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
    public override IEnumerator BeginTest()
    {
        _turnBasedCombatHandler.OnNewTurn += SetupEnemyPokemonState;
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase(0,"Enemies should be immune to Brick Break",
            () => enemy.pokemon.hp >= enemy.pokemon.maxHp);
        
        _testCaseHandler.AddTestCase(2,"Enemies should be hit by Brick Break",
            () => enemy.pokemon.hp < enemy.pokemon.maxHp);
        
        _testCaseHandler.AddTestCase(5,"Enemies should be immune to Brick Break",
            () => enemy.pokemon.hp >= enemy.pokemon.maxHp);
        
        _testCaseHandler.AddTestCase(6,"Foresight should remove evasion buff",
            () => enemy.pokemon.evasion <= 100);
        
        _testCaseHandler.AddTestCase(7,"Enemies should be hit by Brick Break",
            () => enemy.pokemon.hp < enemy.pokemon.maxHp);

        yield return HandleBattleState();
        onTestResult.Invoke();
    }
    private void SetupEnemyPokemonState()
    {
        _turnBasedCombatHandler.OnNewTurn -= SetupEnemyPokemonState;
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemon.types.Clear();
        var ghostType = Resources.Load<Type>(DirectoryHandler.GetDirectory(AssetDirectory.Types) + PokemonType.Ghost);
        enemy.pokemon.types.Add(ghostType);
        enemy.pokemon.statModifiers.Add(new StatChangeData(Stat.Evasion,1));
        enemy.pokemon.evasion = 133f;
    }
    
    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        testingHandler.LogMessage($"Health of enemy: {enemy.pokemon.hp}" +
                                  $"/{enemy.pokemon.maxHp}",TestLogType.Health);
        
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
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            EndTest(false);
        }
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
        LogImmunityState(enemy);
        
        _sequencer.CallNextAction();
    }
}
