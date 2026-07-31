using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MoveTestActionSequencer : TestActionSequencer
{
    private BattleHandler _battleHandler;
    
    public MoveTestActionSequencer(ServiceContainer container, int numSequenceRepetitions = 0)
        : base(numSequenceRepetitions)
    {
        _battleHandler = container.Resolve<BattleHandler>();
    }
    
    public void UseMove(int currentMoveUsageIndex = 0)
    {
        var playerParticipant = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var move = playerParticipant.pokemon.moveSet[currentMoveUsageIndex];
        move.isSureHit = true;
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