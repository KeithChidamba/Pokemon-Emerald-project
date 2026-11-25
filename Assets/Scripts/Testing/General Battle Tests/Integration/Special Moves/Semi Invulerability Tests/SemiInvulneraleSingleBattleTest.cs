using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SemiInvulnerableSingleBattleTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private MoveSequenceHandler _moveUsageHandler;

    private MoveTestActionSequencer _sequencer;

    //test cases are keyed by ROUND (0 based), not by sequence index, because
    //semi-invulnerable participants are auto-advanced and never prompt on their hit round
    private readonly Dictionary<int, List<TestCaseCondition>> _roundCases = new();
    private int _round;
    private string _enemyOriginalMoveName;

    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();

        _sequencer = new MoveTestActionSequencer(container);

        testName = "Semi Invulnerability Single Battle Test";
        testExitCondition = TestCompletionCondition.EndManually;

        /*Actions are only consumed on rounds where the player is actually prompted.
          Hit rounds are resolved automatically by the engine.

          Round 1: Fly charge (enemy tackle must fail)
          Round 2: automatic Fly hit
          Round 3: Fly charge, enemy uses a move that can hit a flying target
          Round 4: automatic Fly hit
          Round 5: free switch to the partner, enemy skips
          Round 6: Dig charge (enemy tackle must fail)
          Round 7: automatic Dig hit
          Round 8: Dig charge, enemy uses a move that can hit a digging target
          Round 9: automatic Dig hit*/
        _sequencer.AddAction(AttackWithNaturalEnemy);
        _sequencer.AddAction(AttackWithEnemyCounter);
        _sequencer.AddAction(HijackEnemyForFreeSwitch);
        _sequencer.AddAction(AttackWithNaturalEnemy);
        _sequencer.AddAction(AttackWithEnemyCounter);

        _moveUsageHandler.OnDamageModified += CheckForInvulnerableDamageEffect;
        _turnBasedCombatHandler.OnTurnEventsCompleted += ReassertSureHit;
    }

    #region Actions
    private void CacheEnemyMoveName()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        _enemyOriginalMoveName ??= enemy.Pokemon.moveSet[0].moveName;
    }

    private void Attack()
    {
        //priority makes the test order reliable
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        currentParticipant.Pokemon.moveSet[0].priority = 100;
        _sequencer.UseMove();
    }

    private void AttackWithNaturalEnemy()
    {
        CacheEnemyMoveName();
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);

        //undo any hijack / disguise so the enemy uses its normal move (which must fail)
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Natural);
        enemy.Pokemon.moveSet[0].moveName = _enemyOriginalMoveName;

        Attack();
    }

    private void AttackWithEnemyCounter()
    {
        CacheEnemyMoveName();
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var semiData = player.Pokemon.moveSet[0]
            .GetDynamicModule<SemiInvulnerabilityInfo>().semiInvulnerabilities;
        var movesThatCounter = semiData.Select(data => data.moveName).ToList();

        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Natural);

        //enemy will use its normal move but disguised as a move that can hit the semi-invulnerable target
        enemy.Pokemon.moveSet[0].moveName = NameDB.GetMoveName(movesThatCounter[0]);
        enemy.Pokemon.moveSet[0].isSureHit = true;
        testingHandler.LogMessage($"changed enemy move to {enemy.Pokemon.moveSet[0].moveName}",
            TestLogType.Information);

        Attack();
    }

    private void HijackEnemyForFreeSwitch()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(ForceEnemySkip);
        _pokemonPartyHandler.SwapToPartner();
        return;

        void ForceEnemySkip()
        {
            _turnBasedCombatHandler.SaveEmptyTurn(_battleHandler.GetCurrentParticipant().participantKey);
        }
    }

    /// <summary>
    /// Runs after the next round's semi-invulnerable turn has been pre-added.
    /// The pre-added turn shares the Move reference, so this applies to the upcoming hit turn.
    /// </summary>
    private void ReassertSureHit()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var turnData = player.semiInvulnerabilityData.turnData;
        if (turnData?.move == null) return;

        turnData.move.isSureHit = true;
        turnData.move.priority = 100;
    }
    #endregion

    private void CheckForInvulnerableDamageEffect(DamageCalculationModifier modifier, float initialDamage,
        float modifiedDamage)
    {
        if (modifier != DamageCalculationModifier.SemiInvulnerable) return;

        testingHandler.LogMessage("Semi-Invulnerability damage change is in effect", TestLogType.Information);
        var increase = modifiedDamage > initialDamage;

        var message = increase
            ? $"vulnerability increased damage from {initialDamage} to {modifiedDamage}"
            : $"vulnerability reduced damage from {initialDamage} to {modifiedDamage}";
        testingHandler.LogMessage(message, TestLogType.Calculation);
    }

    private void AddRoundCase(int round, string message, System.Func<bool> condition)
    {
        _roundCases[round] = new List<TestCaseCondition> { new(message, condition) };
    }

    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);

        bool PlayerFull() => player.Pokemon.hp >= player.Pokemon.maxHp;
        bool PlayerDamaged() => player.Pokemon.hp < player.Pokemon.maxHp;
        bool EnemyDamaged() => enemy.Pokemon.hp < enemy.Pokemon.maxHp;

        //fly
        AddRoundCase(0, "Player untouched while charging Fly", PlayerFull);
        AddRoundCase(1, "Fly hit turn damaged the enemy", EnemyDamaged);
        AddRoundCase(2, "Counter move damaged the semi-invulnerable player", PlayerDamaged);
        AddRoundCase(3, "Fly hit turn damaged the enemy", EnemyDamaged);
        //round 4 is the free switch, nothing to verify
        //dig
        AddRoundCase(5, "Player untouched while charging Dig", PlayerFull);
        AddRoundCase(6, "Dig hit turn damaged the enemy", EnemyDamaged);
        AddRoundCase(7, "Counter move damaged the semi-invulnerable player", PlayerDamaged);
        AddRoundCase(8, "Dig hit turn damaged the enemy", EnemyDamaged);

        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);

        testingHandler.LogMessage($"Health of enemy: {enemy.Pokemon.hp}/{enemy.Pokemon.maxHp}",
            TestLogType.Health);
        testingHandler.LogMessage($"Health of player: {player.Pokemon.hp}/{player.Pokemon.maxHp}",
            TestLogType.Health);

        var round = _round++;
        if (_roundCases.TryGetValue(round, out var conditions))
        {
            TestCaseHandler.ValidateTestCases(testingHandler, new TestCase(round, conditions),
                FinishRound, TestCaseFailed);
        }
        else
        {
            FinishRound();
        }
        return;

        void FinishRound()
        {
            //make test more reliable, prevent conflicting outcomes
            enemy.Pokemon.hp = enemy.Pokemon.maxHp;
            player.Pokemon.hp = player.Pokemon.maxHp;

            //all actions used AND the last hit turn has resolved
            var stillCharged = player.isSemiInvulnerable || player.semiInvulnerabilityData.executionTurn;
            if (_sequencer.SequenceComplete() && !stillCharged)
            {
                Unsubscribe();
                EndTest(true);
            }
        }

        void TestCaseFailed()
        {
            Unsubscribe();
            EndTest(false);
        }
    }

    private void Unsubscribe()
    {
        _moveUsageHandler.OnDamageModified -= CheckForInvulnerableDamageEffect;
        _turnBasedCombatHandler.OnTurnEventsCompleted -= ReassertSureHit;
    }

    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey != BattleParticipantKey.Player) return;

        //charged participants are auto-advanced by the engine, never consume an action for them
        if (currentParticipant.isSemiInvulnerable
            || currentParticipant.semiInvulnerabilityData.executionTurn) return;

        if (_sequencer.SequenceComplete()) return;
        _sequencer.CallNextAction();
    }
}