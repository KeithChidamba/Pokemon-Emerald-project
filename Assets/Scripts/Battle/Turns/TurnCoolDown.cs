using System;
[Serializable]
public class TurnCoolDown
{
    public int numTurns;
    public Turn turnData;
    public string coolDownMessage;
    public bool canDisplayMessage;
    public BattleParticipant participant;
    public bool isCoolingDown;
    public bool isExecutionTurn;
    private MoveSequenceHandler _moveUsageHandler;

    public TurnCoolDown(BattleParticipant participantParent,MoveSequenceHandler moveUsageHandler)
    {
        UpdateCoolDown(0, null, display : false,coolingDown: false);
        _moveUsageHandler = moveUsageHandler;
        participant = participantParent;
    }
    public void UpdateCoolDown(int turns,Turn turn, string message=""
        , bool display = true,bool coolingDown=true)
    {
        turnData = turn;
        numTurns = turns;
        coolDownMessage = message;
        canDisplayMessage = display;
        isCoolingDown = coolingDown;
    }
    public void ResetState()
    {
        _moveUsageHandler.OnMoveHit -= StoreDamage;
        numTurns = 0;
        coolDownMessage = string.Empty;
        turnData = null;
        canDisplayMessage = false;
        isCoolingDown = false;
        isExecutionTurn = false;
    }
    public void StoreDamage(BattleParticipant attacker,BattleParticipant victim,Move moveUsed,float finalDamage)
    {
        if (victim != participant) return;
       turnData.move.moveDamage += finalDamage;
    }
}
