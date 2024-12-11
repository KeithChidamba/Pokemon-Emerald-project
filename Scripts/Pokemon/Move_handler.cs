using UnityEngine;

public class Move_handler:MonoBehaviour
{
    public bool Doing_move = false;
    public static Move_handler instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    public void Do_move(Turn turn)
    {
        Dialogue_handler.instance.Write_Info(turn.attacker_.Pokemon_name+" used "+turn.move_.Move_name+" on "+turn.victim_.Pokemon_name+"!","Battle info");
        Dialogue_handler.instance.Dialouge_off(1.8f); 
        Invoke(nameof(tests),2f);
        /*while (Doing_move)
        {
            await Task.Yield();
        }*/
        
        //damage pokemon
        //call appropriate move for move effect
        //invoke move_name+effect methods
    }

    void tests()
    {
        Dialogue_handler.instance.Write_Info("The move effect is operating", "Details");
        Dialogue_handler.instance.Dialouge_off(2f);
        Invoke(nameof(Move_done),2f);
    }

    void Move_done()
    {
        Doing_move = false;
    }
}
