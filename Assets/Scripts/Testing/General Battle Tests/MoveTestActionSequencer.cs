using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MoveTestActionSequencer : TestActionSequencer
{
    private BattleHandler _battleHandler;
    
    public MoveTestActionSequencer(ServiceContainer container)
    {
        _battleHandler = container.Resolve<BattleHandler>();
    }

    public override int GetTestCaseIndex()
    {
        //test cases are check at the end of a turn
        if (_battleHandler.isDoubleBattle)
        {
            //At this point the index will always be a multiple of 2
            //because of how test actions are counted in double battle
            return (CurrentSequenceIndex / 2) - 1;
        }
        //sequence index +1, so reduce it
        return CurrentSequenceIndex - 1;
    }
    public void UseMove(int currentMoveUsageIndex = 0,bool isSureHit=true)
    {
        var playerParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var move = playerParticipant.pokemon.moveSet[currentMoveUsageIndex];
        move.isSureHit = isSureHit;
        _battleHandler.UseMove(move,playerParticipant, BattleParticipantKey.Enemy);
    }
    public void UseMoveOnSpecific(int currentMoveUsageIndex, BattleParticipantKey playerKey
        ,BattleParticipantKey enemyKey)
    {
        var playerParticipant = _battleHandler.GetParticipant(playerKey);
        var move = playerParticipant.pokemon.moveSet[currentMoveUsageIndex];
        move.isSureHit = true;
        _battleHandler.UseMove(move,playerParticipant, enemyKey);
    }
}