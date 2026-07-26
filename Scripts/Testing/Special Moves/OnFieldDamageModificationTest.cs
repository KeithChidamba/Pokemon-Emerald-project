using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnFieldDamageModificationTest : BattleMoveUsageTest
{
    private TestActionSequencer _sequencer;
    private Move_handler _moveUsageHandler;
    private Battle_handler _battleHandler;
    
    private bool _damageWasChanged;
    public override void Inject(ServiceContainer serviceContainer)
    {
        _moveUsageHandler = serviceContainer.Resolve<Move_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        
        container = serviceContainer;
        testName = "On Field Damage Modification Test";
        testExitCondition = TestCompletionCondition.EndManually;
        _sequencer = new TestActionSequencer();
       // _sequencer.AddAction(() => UseMove(1),false);
    }
    
    public override IEnumerator BeginTest()
    {
      
        yield return HandleBattleState();
        onTestResult.Invoke();
    }

    protected override void DetermineSuccess()
    {
        //_moveUsageHandler.OnDamageModified -= CheckForBarrierEffect;
        if (_sequencer.SequenceComplete())
        {
            SetStatus(true);
            EndTest();
        }
        // if (_damageWasChanged)
        // {
        //     if( _currentBarrierToTest == BarrierType.Special)
        //     {
        //         //start new turn and use reflect
        //         _damageWasChanged = false;
        //         _currentBarrierToTest = BarrierType.Physical;
        //     }
        //     else
        //     {
        //         SetStatus(true);
        //         EndTest();
        //     }
        // }
        // else
        // {
        //     testingHandler.LogMessage($"barrier Test failed at {_currentBarrierToTest}",TestLogType.Error);
        //     SetStatus(false);
        //     EndTest();
        // }
    }
    
    protected override void DetermineTurnUsage()
    {
        _sequencer.RepeatSequence();
        // if (_sequencer.CurrentSequenceIndex == 0)
        // {
        //     //first turn
        //     enemy.pokemon.types.Clear();
        //     var ghostType = Resources.Load<Type>(DirectoryHandler.GetDirectory(AssetDirectory.Types) + PokemonType.Ghost);
        //     enemy.pokemon.types.Add(ghostType);
        //     enemy.pokemon.buffAndDebuffs.Add(new Buff_Debuff(Stat.Evasion,1));
        //     enemy.pokemon.evasion = 133f;
        // }
        // if (_sequencer.CanDisplayCurrentLogs())
        // { 
        //     LogImmunityState(enemy);
        // }
        // if (_battleHandler.GetCurrentParticipant().participantKey == BattleParticipantKey.Player)
        // {
        //     //use barrier creation move : light screen or reflect
        //     var mudkipParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        //     var move = mudkipParticipant.pokemon.moveSet[0];
        //     move.isSureHit = true;
        //     _moveUsageHandler.OnDamageModified += CheckForBarrierEffect;
        //     _battleHandler.UseMove(move,mudkipParticipant, BattleParticipantKey.Enemy);
        // }
    }
    
}
