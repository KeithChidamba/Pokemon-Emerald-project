using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[CreateAssetMenu(fileName = "berryTree", menuName = "berryTree")]
public class BerryTreeData : ScriptableObject
{
     public bool isPlanted;
     public int minYield;
     public int maxYield;
     public bool currentStageNeedsWater;
     public int currentStageProgress;
     public float minutesSinceLastStage;
     public int numStagesWatered;
     
     public string itemAssetName;
     public Item berryItem;
     public int treeIndex;
     public int minutesPerStage;
     public List<BerrySpriteData> spriteData = new();

     public string lastLogin;
    
     public DateTime GetLastLogin()
     {
          return DateTime.Parse(lastLogin, null, System.Globalization.DateTimeStyles.RoundtripKind);
     }

     public void SetLastLogin(DateTime dt)
     {
          lastLogin = dt.ToString("o"); // "o" = ISO 8601
     }
     
     public Sprite[] GetTreeSprite()
     {
          return spriteData.First(s => s.growthStageNumber == currentStageProgress).growthStageSprites;
     }
     
}
