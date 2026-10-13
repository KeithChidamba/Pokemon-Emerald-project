using System;
using System.Collections.Generic;

[Serializable]
public class NpcAnimationData
{
    public SpriteDataForNpc spriteData;
    public List<NpcMovementDirection> movementDirections = new ();
    public bool isIdle;
}
public enum MovementDirection
{
    //aligned with animator tree values
    Down=1,Up=2,Left=3,Right=4
}