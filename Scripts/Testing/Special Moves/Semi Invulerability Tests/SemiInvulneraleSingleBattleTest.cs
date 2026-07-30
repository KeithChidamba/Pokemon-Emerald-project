using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SemiInvulnerableSingleBattleTest : BattleMoveUsageTest
{
    private Battle_handler _battleHandler;
    private Pokemon_party _pokemonPartyHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;

    private MoveTestActionSequencer _sequencer;

    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<Battle_handler>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        
        _sequencer = new MoveTestActionSequencer(container);
        testName = "Semi Invulnerability Single Battle Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(() => _sequencer.UseMove());//fly
        _sequencer.AddAction(EnsureHitAndSkipTurn);
        _sequencer.AddAction(_pokemonPartyHandler.SwapToPartner);
        _sequencer.AddAction(() => _sequencer.UseMove());//dig
        _sequencer.AddAction(EnsureHitAndSkipTurn);
    }

    private void EnsureHitAndSkipTurn()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        //semi-invulnerable logic removes sure hit when it's about to
        //deal damage, so revert it for testing purposes
        player.semiInvulnerabilityData.turnData.move.isSureHit = true;
        player.semiInvulnerabilityData.turnData.move.priority = 0;
        _turnBasedCombatHandler.NextTurn();
    }
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
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
    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey != BattleParticipantKey.Player) return;
        _sequencer.CallNextAction();
    }
}
