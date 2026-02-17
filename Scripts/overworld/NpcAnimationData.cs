using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Overworld/Npc Animation Data")]
public class NpcAnimationData : ScriptableObject
{
    public List<NpcSpriteData> spriteData = new ();
    
    public bool IsVerticalMovement(NpcSpriteData data)
    {
        return (int)data.direction > 1;
    }
    
    public int GetDirectionAsMagnitude(NpcSpriteData data)
    {
        switch (data.direction)
        {
            case NpcAnimationDirection.Down:
            case NpcAnimationDirection.Left:
                return -1 * data.numTilesTOTravel;
            case NpcAnimationDirection.Up:
            case NpcAnimationDirection.Right:
                return 1 * data.numTilesTOTravel;
        }
        return 0;
    }
}


public enum NpcAnimationDirection
{
    Right,Left,Up,Down
}