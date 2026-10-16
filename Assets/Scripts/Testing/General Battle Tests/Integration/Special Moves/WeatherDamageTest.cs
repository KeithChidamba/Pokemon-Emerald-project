using System.Collections;
using UnityEngine;

public class WeatherDamageTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private BattleOperations _battleOperations;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    
    private MoveTestActionSequencer _sequencer;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _battleOperations = container.Resolve<BattleOperations>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        testName = "Weather Damage Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        _sequencer = new MoveTestActionSequencer(container);
        
        //Give 1 enemy the ice type, to test hail's selective damage
        _sequencer.AddAction(GiveEnemyIceType);
        //Give 2 enemies some types that are safe in sandstorm
        _sequencer.AddAction(GiveEnemyProtectedTypes);
        //use tailwhip as turn buffer, to take weather damage
        _sequencer.AddAction(() => _sequencer.UseMove(2));
        //remove to test buff removal
        _sequencer.AddAction(RemoveSandStorm);
    }

    private void RemoveSandStorm()
    {
        var newWeather = new WeatherCondition(Weather.Clear);
        _turnBasedCombatHandler.ChangeWeather(newWeather);
        _sequencer.UseMove(2);
    }
    private void GiveEnemyIceType()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.Pokemon.types.Clear();
        var iceType = Resources.Load<PokemonTypeData>(DirectoryHandler.GetDirectory(AssetDirectory.Types) + PokemonType.Ice);
        enemy.Pokemon.types.Add(iceType);
        
        _sequencer.UseMove();//Hail
    }
    private void GiveEnemyProtectedTypes()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var enemyPartner = _battleHandler.GetParticipant(BattleParticipantKey.EnemyPartner);
        
        enemy.Pokemon.types.Clear();
        enemyPartner.Pokemon.types.Clear();
        
        var rockType = Resources.Load<PokemonTypeData>(DirectoryHandler.GetDirectory(AssetDirectory.Types) + PokemonType.Rock);
        var groundType = Resources.Load<PokemonTypeData>(DirectoryHandler.GetDirectory(AssetDirectory.Types) + PokemonType.Ground);
        var steelType = Resources.Load<PokemonTypeData>(DirectoryHandler.GetDirectory(AssetDirectory.Types) + PokemonType.Steel);
        enemy.Pokemon.types.Add(rockType);//to test special defense boost from sandstorm
        enemy.Pokemon.types.Add(groundType);
        enemyPartner.Pokemon.types.Add(steelType);
        _battleOperations.OnStatChangeApplied += AwaitStatChangeAddition;
        _sequencer.UseMove(1);//Sandstorm

    }
    private void AwaitStatChangeAddition(StatChangeOperationData operationData)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        if (operationData.statChangeData.receiver.participantKey != enemy.participantKey)
        {
            //only this enemy will receive the rock type buff for special defense
            return;
        }
        
        testingHandler.LogMessage($"The {operationData.finalStatData.statName} stat is at stage {operationData.finalStatData.stage}" +
                                  $" and is affecting {operationData.statChangeData.receiver.Pokemon.pokemonName}"
            ,TestLogType.Information);
    }
    public override IEnumerator BeginTest()
    {
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
    protected override void DetermineSuccess()
    {
        if (_sequencer.SequenceComplete())
        {
            _battleOperations.OnStatChangeApplied -= AwaitStatChangeAddition;
            
            var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
            var enemyPartner = _battleHandler.GetParticipant(BattleParticipantKey.EnemyPartner);
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            var testPassed = enemy.Pokemon.hp >= enemy.Pokemon.maxHp //should be unaffected
                             && enemyPartner.Pokemon.hp < enemyPartner.Pokemon.maxHp //hail damage
                             && player.Pokemon.hp < player.Pokemon.maxHp; //both weather damage cases
           
            EndTest(testPassed);
        }
    }

    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey != BattleParticipantKey.Player) return;
        
        testingHandler.LogMessage($"Health of player: {currentParticipant.Pokemon.hp}" +
                                  $"/{currentParticipant.Pokemon.maxHp}",TestLogType.Health);
        
        _sequencer.CallNextAction();
    }
}
