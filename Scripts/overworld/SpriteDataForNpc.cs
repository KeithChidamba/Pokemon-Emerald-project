using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[CreateAssetMenu(menuName = "Overworld/Npc Sprite Data")]
public class SpriteDataForNpc : ScriptableObject
{
    public List<NpcSpriteData> spriteDataList = new ();

    public NpcSpriteData GetSpriteData(MovementDirection direction)
    {
        return  spriteDataList.First(spriteData => spriteData.direction == direction);
    }
   
}
