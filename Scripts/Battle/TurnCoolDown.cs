using System;
[Serializable]
public class TurnCoolDown
{
    public int numTurns;
    public Turn turnData;
    public string message;
    public bool displayMessage;
    public Battle_Participant participant;
    public bool isCoolingDown;
    public bool executeTurn;
    private Move_handler _moveUsageHandler;

    public TurnCoolDown(Battle_Participant participantParent,Move_handler moveUsageHandler)
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
        displayMessage = display;
        isCoolingDown = coolingDown;
    }
    public void ResetState()
    {
        _moveUsageHandler.OnDamageDeal -= StoreDamage;
        numTurns = 0;
        message = string.Empty;
        turnData = null;
        displayMessage = false;
        isCoolingDown = false;
        executeTurn = false;
    }
    public void StoreDamage(float damage,Battle_Participant victim)
    {
        if (victim != participant) return;
       turnData.move.moveDamage += damage;
    }
}
