using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Overworld/Npc Animation Data")]
public class NpcAnimationData : ScriptableObject
{
    public SpriteDataForNpc spriteData;
    public List<NpcMovementDirection> movementDirections = new ();
    
    public bool IsVerticalMovement(NpcAnimationDirection direction)
    {
        return (int)direction > 1;
    }
    
    public int GetDirectionAsMagnitude(NpcMovementDirection data)
    {
        switch (data.direction)
        {
            case NpcAnimationDirection.Down:
            case NpcAnimationDirection.Left:
                return -1 * data.numTilesToTravel;
            case NpcAnimationDirection.Up:
            case NpcAnimationDirection.Right:
                return 1 * data.numTilesToTravel;
        }
        return 0;
    }
}


public enum NpcAnimationDirection
{
    Right,Left,Up,Down
}