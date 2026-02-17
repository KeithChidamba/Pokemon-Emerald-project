using System;
using UnityEngine;

[Serializable]
public struct NpcSpriteData
{
    public Sprite idleSprite;
    public Sprite[] spritesForDirection;
    public NpcAnimationDirection direction;
}