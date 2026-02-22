using System;
using UnityEngine;

[Serializable]
public struct NpcSpriteData
{
    public Sprite idleSprite;
    public Sprite[] spritesForDirection;
    public MovementDirection direction;
}