using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SemiInvulnerableDoubleBattleTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;

    private MoveTestActionSequencer _sequencer;

    private readonly Dictionary<int, List<TestCaseCondition>> _roundCases = new();
    private int _round;

    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        /*This test exists to just test that single battle logic doesn't
        break here*/
        testName = "Semi Invulnerability Double Battle Test";
        testExitCondition = TestCompletionCondition.EndManually;

        _sequencer = new MoveTestActionSequencer(container);

        /*Round 1: both players are prompted and charge (the only two actions)
          Round 2: both hit turns are resolved automatically by the engine*/

        //fly
        _sequencer.AddAction(() =>
        {
            SetPriority(BattleParticipantKey.Player);
            _sequencer.UseMove();
        });
        //dig
        _sequencer.AddAction(() =>
        {
            SetPriority(BattleParticipantKey.PlayerPartner);
            _sequencer.UseMoveOnSpecific(
                0,
                BattleParticipantKey.PlayerPartner,
                BattleParticipantKey.EnemyPartner);
        });

        _turnBasedCombatHandler.OnTurnEventsCompleted += ReassertSureHit;
    }

    private void SetPriority(BattleParticipantKey key)
    {
        //charge first so the enemy attacks land while the target is semi-invulnerable
        _battleHandler.GetParticipant(key).Pokemon.moveSet[0].priority = 100;
    }

    /// <summary>
    /// Runs after the next round's semi-invulnerable turns have been pre-added.
    /// The pre-added turns share the Move reference, so this applies to the upcoming hit turns.
    /// </summary>
    private void ReassertSureHit()
    {
        ReassertFor(BattleParticipantKey.Player);
        ReassertFor(BattleParticipantKey.PlayerPartner);
        return;

        void ReassertFor(BattleParticipantKey key)
        {
            var turnData = _battleHandler.GetParticipant(key).semiInvulnerabilityData.turnData;
            if (turnData?.move == null) return;

            turnData.move.isSureHit = true;
            turnData.move.priority = 100;
        }
    }

    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var playerPartner = _battleHandler.GetParticipant(BattleParticipantKey.PlayerPartner);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var enemyPartner = _battleHandler.GetParticipant(BattleParticipantKey.EnemyPartner);

        //a fainted participant counts as damaged
        bool WasDamaged(BattleParticipant p) => p.Pokemon == null || p.Pokemon.hp < p.Pokemon.maxHp;
        bool IsUntouched(BattleParticipant p) => p.Pokemon != null && p.Pokemon.hp >= p.Pokemon.maxHp;

        //round 1: enemy attacks landed on semi-invulnerable targets
        _roundCases[0] = new List<TestCaseCondition>
        {
            new("Player was untouched while flying", () => IsUntouched(player)),
            new("Partner was untouched while underground", () => IsUntouched(playerPartner)),
        };
        //round 2: both hit turns resolved
        _roundCases[1] = new List<TestCaseCondition>
        {
            new("Enemy was attacked", () => WasDamaged(enemy)),
            new("Enemy partner was attacked", () => WasDamaged(enemyPartner)),
        };

        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        var round = _round++;
        if (_roundCases.TryGetValue(round, out var conditions))
        {
            TestCaseHandler.ValidateTestCases(testingHandler, new TestCase(round, conditions),
                CheckTestEnd, TestCaseFailed);
        }
        else
        {
            CheckTestEnd();
        }
        return;

        void CheckTestEnd()
        {
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            var partner = _battleHandler.GetParticipant(BattleParticipantKey.PlayerPartner);

            //sequence is already complete after round 1, so also wait for the hit turns to resolve
            var stillCharged = IsCharged(player) || IsCharged(partner);
            if (_sequencer.SequenceComplete() && !stillCharged)
            {
                _turnBasedCombatHandler.OnTurnEventsCompleted -= ReassertSureHit;
                EndTest(true);
            }
        }

        void TestCaseFailed()
        {
            _turnBasedCombatHandler.OnTurnEventsCompleted -= ReassertSureHit;
            EndTest(false);
        }
    }

    private static bool IsCharged(BattleParticipant participant)
    {
        return participant.isSemiInvulnerable || participant.semiInvulnerabilityData.executionTurn;
    }

    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey is BattleParticipantKey.Enemy or BattleParticipantKey.EnemyPartner)
        {
            return;
        }

        //charged participants are auto-advanced by the engine, never consume an action for them
        if (IsCharged(currentParticipant)) return;

        if (_sequencer.SequenceComplete()) return;
        _sequencer.CallNextAction();
    }
}