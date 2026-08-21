using System;
using UnityEngine;
[Serializable]
public struct NpcMovementDirection
{
    public MovementDirection direction;
    public int numTilesToTravel;

    public NpcMovementDirection(MovementDirection direction, int numTilesToTravel)
    {
        this.direction = direction;
        this.numTilesToTravel = numTilesToTravel;
    }
}
