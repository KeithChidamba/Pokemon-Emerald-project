using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Overworld/Npc Animation Data")]
public class NpcAnimationData : ScriptableObject
{
    public SpriteDataForNpc spriteData;
    public List<NpcMovementDirection> movementDirections = new ();
    public bool isIdle;
    public bool IsVerticalMovement(MovementDirection direction)
    {
        return direction is MovementDirection.Up or MovementDirection.Down;
    }
    
    public int GetDirectionAsMagnitude(NpcMovementDirection data)
    {
        switch (data.direction)
        {
            case MovementDirection.Down:
            case MovementDirection.Left:
                return 1 * data.numTilesToTravel;
            case MovementDirection.Up:
            case MovementDirection.Right:
                return -1 * data.numTilesToTravel;
        }
        return 0;
    }
}


public enum MovementDirection
{
    //aligned with animator tree values
    Down=1,Up=2,Left=3,Right=4
}