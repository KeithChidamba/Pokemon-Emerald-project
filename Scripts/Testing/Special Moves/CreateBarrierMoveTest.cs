using System.Collections;
using UnityEngine;

public class CreateBarrierMoveTest : BattleMoveUsageTest
{
    private Move_handler _moveUsageHandler;
    private bool _damageWasChanged;

    private enum BarrierType
    {
        Special,Physical
    }
    private BarrierType _currentBarrierToTest;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        _moveUsageHandler = serviceContainer.Resolve<Move_handler>();
        container = serviceContainer;
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
                _damageWasChanged = false;
                _currentBarrierToTest = BarrierType.Physical;
                //start new turn and use reflect
            }
            else
            {
                SetStatus(true);
                EndTest();
            }
        }
        else
        {
            testingHandler.LogMessage($"barrier Test failed at {_currentBarrierToTest}",LogType.Error);
            SetStatus(false);
            EndTest();
        }
    }
    
    protected override void DetermineMoveUsage()
    {
        if (battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        {
            //use barrier creation move : light screen or reflect
            var mudkipParticipant = battleHandler.GetParticipant(BattleParticipantKey.Player);
            var barrierMove = _currentBarrierToTest == BarrierType.Special
                ? mudkipParticipant.pokemon.moveSet[0] //light screen
                : mudkipParticipant.pokemon.moveSet[1];//reflect
            
            var treeckoParticipant1 = battleHandler.GetParticipant(BattleParticipantKey.Enemy);
            var treeckoParticipant2 = battleHandler.GetParticipant(BattleParticipantKey.EnemyPartner);
            treeckoParticipant1.pokemon.moveSet.Clear();
            treeckoParticipant2.pokemon.moveSet.Clear();

            var moveName = _currentBarrierToTest == BarrierType.Physical
                ? NameDB.GetMoveName(LearnSetMoveName.Tackle)
                : NameDB.GetMoveName(LearnSetMoveName.LeafBlade);
            
            var assetPath = DirectoryHandler.GetDirectory(AssetDirectory.Moves) + moveName;
            var moveFromAsset = Resources.Load<Move>(assetPath);
            var newMove = InstanceFactory.CreateMove(moveFromAsset);
            var newMove2 = InstanceFactory.CreateMove(moveFromAsset);
            treeckoParticipant1.pokemon.moveSet.Add(newMove);
            treeckoParticipant2.pokemon.moveSet.Add(newMove2);
            
            //make sure 2 treecko enemies hit the barriers and dont miss
            treeckoParticipant1.pokemon.moveSet[0].isSureHit = true; 
            treeckoParticipant2.pokemon.moveSet[0].isSureHit = true;
            
            _moveUsageHandler.OnDamageModified += CheckForBarrierEffect;
            battleHandler.UseMove(barrierMove,mudkipParticipant, BattleParticipantKey.Enemy);
        }
        
        if (battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.PlayerPartner)
        {
            var partnerParticipant = battleHandler.GetParticipant(BattleParticipantKey.PlayerPartner);
            var thunderBolt = partnerParticipant.pokemon.moveSet[0];
            thunderBolt.moveDamage = 1f;//don't kill enemy
            battleHandler.UseMove(thunderBolt,partnerParticipant,BattleParticipantKey.EnemyPartner);
        }
    }

    void CheckForBarrierEffect(DamageCalculationModifier modifier,float initialDamage,float modifiedDamage)
    {
        if (modifier == DamageCalculationModifier.Barrier)
        {
            _damageWasChanged = modifiedDamage < initialDamage;
            if(_damageWasChanged)
            {
                testingHandler.LogMessage($"barrier reduced damage from {initialDamage} to {modifiedDamage}",LogType.Calculation);
            }
        }
    }
}
