using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "berryTree", menuName = "Overworld/Berry tree")]
public class BerryTreeData : ScriptableObject
{
     public Item berryItem; 
     public List<BerrySpriteData> spriteData = new();
     public int minYield;
     public int maxYield; 
     public int minutesPerStage;
     
     public bool currentStageNeedsWater;
     public int currentStageProgress;
     public float minutesSinceLastStage;
     public int numStagesWatered;
     public bool isPlanted;
     [HideInInspector]public string itemAssetName;
     [HideInInspector]public int soilIndex;
     [HideInInspector]public string lastLogin;
    
     public DateTime GetLastLogin()
     {
          return DateTime.Parse(lastLogin, null, System.Globalization.DateTimeStyles.RoundtripKind);
     }
     public Sprite[] GetTreeSprites()
     {
          return spriteData.First(s => s.growthStageNumber == currentStageProgress).growthStageSprites;
     }
}
