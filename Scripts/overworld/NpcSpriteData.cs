using System;
using UnityEngine;


[Serializable]
public struct NpcSpriteData
{
    public Sprite[] spritesForDirection;
    public NpcAnimationDirection direction;
    public int numTilesTOTravel;
}