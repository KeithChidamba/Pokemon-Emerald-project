using System.Collections;
using UnityEngine;

public class CreateBarrierMoveTest : BattleBasedTest
{
    private MoveSequenceHandler _moveUsageHandler;
    private BattleHandler _battleHandler;
    
    private bool _damageWasChanged;

    private enum BarrierType
    {
        Special,Physical
    }
    private BarrierType _currentBarrierToTest;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _moveUsageHandler = serviceContainer.Resolve<MoveSequenceHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        
        testName = "Create Barrier Test";
        testExitCondition = TestCompletionCondition.EndManually;
    }
    
    public override IEnumerator BeginTest()
    {
        _currentBarrierToTest = BarrierType.Special;
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        _moveUsageHandler.OnDamageModified -= CheckForBarrierEffect;
        if (_damageWasChanged)
        {
            if( _currentBarrierToTest == BarrierType.Special)
            {
                //start new turn and use reflect
                _damageWasChanged = false;
                _currentBarrierToTest = BarrierType.Physical;
            }
            else
            {
                EndTest(true);
            }
        }
        else
        {
            testingHandler.LogMessage($"barrier Test failed at {_currentBarrierToTest}",TestLogType.Error);
            EndTest(false);
        }
    }
    
    protected override void DetermineTurnUsage()
    {
        if (_battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        {
            //use barrier creation move : light screen or reflect
            var mudkipParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            var barrierMove = _currentBarrierToTest == BarrierType.Special
                ? mudkipParticipant.pokemon.moveSet[0] //light screen
                : mudkipParticipant.pokemon.moveSet[1];//reflect
            
            var treeckoParticipant1 = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
            var treeckoParticipant2 = _battleHandler.GetParticipant(BattleParticipantKey.EnemyPartner);
            treeckoParticipant1.pokemon.moveSet.Clear();
            treeckoParticipant2.pokemon.moveSet.Clear();

            var moveName = _currentBarrierToTest == BarrierType.Physical
                ? NameDB.GetMoveName(MoveName.Tackle)
                : NameDB.GetMoveName(MoveName.LeafBlade);
            
            var assetPath = DirectoryHandler.GetDirectory(AssetDirectory.Moves) + moveName;
            var moveFromAsset = Resources.Load<Move>(assetPath);
            var newMove = InstanceFactory.CreateMove(moveFromAsset);
            var newMove2 = InstanceFactory.CreateMove(moveFromAsset);
            treeckoParticipant1.pokemon.moveSet.Add(newMove);
            treeckoParticipant2.pokemon.moveSet.Add(newMove2);
            
            //make sure 2 treeko enemies hit the barriers and dont miss
            treeckoParticipant1.pokemon.moveSet[0].isSureHit = true; 
            treeckoParticipant2.pokemon.moveSet[0].isSureHit = true;
            
            _moveUsageHandler.OnDamageModified += CheckForBarrierEffect;
            _battleHandler.UseMove(barrierMove,mudkipParticipant, BattleParticipantKey.Enemy);
        }
        
        if (_battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.PlayerPartner)
        {
            var partnerParticipant = _battleHandler.GetParticipant(BattleParticipantKey.PlayerPartner);
            var thunderBolt = partnerParticipant.pokemon.moveSet[0];
            thunderBolt.moveDamage = 1f;//don't kill enemy
            _battleHandler.UseMove(thunderBolt,partnerParticipant,BattleParticipantKey.EnemyPartner);
        }
    }

    void CheckForBarrierEffect(DamageCalculationModifier modifier,float initialDamage,float modifiedDamage)
    {
        if (modifier == DamageCalculationModifier.Barrier)
        {
            _damageWasChanged = modifiedDamage < initialDamage;
            if(_damageWasChanged)
            {
                testingHandler.LogMessage($"barrier reduced damage from {initialDamage} to {modifiedDamage}",TestLogType.Calculation);
            }
        }
    }
}
