using System;
[Serializable]
public class TurnCoolDown
{
    public int numTurns;
    public Turn turnData;
    public string message;
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
    public void UpdateCoolDown(int numTurns,Turn turn, string message=""
        , bool display = true,bool coolingDown=true)
    {
        turnData = turn;
        this.numTurns = numTurns;
        this.message = message;
        canDisplayMessage = display;
        isCoolingDown = coolingDown;
    }
    public void ResetState()
    {
        _moveUsageHandler.OnMoveHit -= StoreDamage;
        numTurns = 0;
        message = string.Empty;
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
