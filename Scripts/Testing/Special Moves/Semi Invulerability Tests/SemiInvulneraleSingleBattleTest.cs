using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SemiInvulnerableSingleBattleTest : BattleMoveUsageTest
{
    private Battle_handler _battleHandler;
    private Pokemon_party _pokemonPartyHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Move_handler _moveUsageHandler;
    
    private MoveTestActionSequencer _sequencer;
    private bool _testPassing;
    private List<Func<(string message,bool result)>> testCases = new();
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<Battle_handler>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _moveUsageHandler = container.Resolve<Move_handler>();
        
        _sequencer = new MoveTestActionSequencer(container,1);
        testName = "Semi Invulnerability Single Battle Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //first fly, then on second iteration dig
        _sequencer.AddAction(AttackFirst);
        _sequencer.AddAction(EnsureHitAndSkipTurn);
        _sequencer.AddAction(AllowEnemyToCounter);
        _sequencer.AddAction(EnsureHitAndSkipTurn);
        _sequencer.AddAction(HijackEnemyForFreeSwitch);
        
        _moveUsageHandler.OnDamageModified += CheckForInvulnerableDamageEffect;
    }

    private void HijackEnemyForFreeSwitch()
    {
        _sequencer.RemoveLastAction();
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.HighJackTurn();
        _pokemonPartyHandler.SwapToPartner();
    }
    private void AttackFirst()
    {
        //To make test case reliable
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        currentParticipant.pokemon.moveSet[0].priority = 100;
        _sequencer.UseMove();
    }
    private void AllowEnemyToCounter()
    {
        //give enemy a move that can negate 
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var semiData = player.pokemon.moveSet[0].GetModule<SemiInvulnerabilityInfo>().semiInvulnerabilities;
        semiData[0].damageMultiplier = 2f;//just for testing damage change
        var movesThatCounter = semiData.Select(data => data.moveName).ToList();
        Debug.Log($"using {movesThatCounter[0]} as counter");
        
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        //enemy will use tackle but disguised as a counter viable move
        enemy.pokemon.moveSet[0].moveName = NameDB.GetMoveName(movesThatCounter[0]);
        enemy.pokemon.moveSet[0].isSureHit = true;
        testingHandler.LogMessage($"changed enemy move to {enemy.pokemon.moveSet[0].moveName}",TestLogType.Information);
        //just in-case
        enemy.pokemon.hp = enemy.pokemon.maxHp * .75f;
        AttackFirst();
    }
    
    private void EnsureHitAndSkipTurn()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        //semi-invulnerable logic removes sure hit when it's about to
        //deal damage, so revert it for testing purposes
        player.semiInvulnerabilityData.turnData.move.isSureHit = true;
        player.semiInvulnerabilityData.turnData.move.priority = 100;
        _turnBasedCombatHandler.NextTurn();
    }
    void CheckForInvulnerableDamageEffect(DamageCalculationModifier modifier,float initialDamage,float modifiedDamage)
    {
        if (modifier == DamageCalculationModifier.SemiInvulnerable)
        {
            testingHandler.LogMessage("Semi-Invulnerability damage change is in effect",  TestLogType.Information);
            var increase = modifiedDamage > initialDamage;
            
            var message = increase
                ? $"vulnerability increased damage from {initialDamage} to {modifiedDamage}"
                : $"vulnerability reduced damage from {initialDamage} to {modifiedDamage}";
            testingHandler.LogMessage(message, TestLogType.Calculation);
        }
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        testCases.Add(() => ("Player has full health",player.pokemon.hp >= player.pokemon.maxHp));
        testCases.Add(() => ("Player has been damaged",player.pokemon.hp < player.pokemon.maxHp));
        
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
        
        var testCaseIndex = _sequencer.CurrentSequenceIndex - 1;
        testCaseIndex = Math.Clamp(testCaseIndex, 0, testCases.Count-1);//account for index refresh of sequencer
       
        var testCaseResult = testCases[testCaseIndex].Invoke();
        
        if (!testCaseResult.result)
        {
            testingHandler.LogMessage($"Test case({testCaseIndex+1}) Failed due to violation" +
                                      $" of {testCaseResult.message}",TestLogType.Information);
            
            _moveUsageHandler.OnDamageModified -= CheckForInvulnerableDamageEffect;
            //test case failed
            SetStatus(false);
            EndTest();
        }
        else
        {
            if (_sequencer.SequenceComplete())
            {
                _moveUsageHandler.OnDamageModified -= CheckForInvulnerableDamageEffect;
                SetStatus(true);
                EndTest();
            }
        }
        
    }
    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey != BattleParticipantKey.Player) return;
        _sequencer.CallNextAction();
    }
}
