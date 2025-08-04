using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapData
{
    private Move _trapMove;
    public string OnTrapMessage;
    public string OnHitMessage;
    public string OnFreeMessage;
    public TrapData(Move move)
    {
        _trapMove = move;
        GetTrapMessage();
    }

    void GetTrapMessage()
    {
        switch(_trapMove.moveName){
            case "Fire Spin":
            OnTrapMessage = " was trapped in the vortex!";
            OnHitMessage = " is hurt by Fire Spin!";
            OnFreeMessage = " was freed from the Fire Spin!";
            break;

            case "Whirlpool":
            OnTrapMessage = " was trapped in a vortex!";
            OnHitMessage = " is hurt by Whirlpool!";
            OnFreeMessage = " was freed from the Whirlpool!";
            break;

            case "Sand Tomb":
            OnTrapMessage = "became trapped by Sand Tomb!";
            OnHitMessage = " is hurt by Sand Tomb!";
            OnFreeMessage = " was freed from the Sand Tomb!";
            break;
        }
    }
}
