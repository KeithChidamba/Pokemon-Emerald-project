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
     
     [HideInInspector]public bool currentStageNeedsWater;
     [HideInInspector]public int currentStageProgress;
     [HideInInspector]public float minutesSinceLastStage;
     [HideInInspector]public int numStagesWatered;
     [HideInInspector]public bool isPlanted;
     [HideInInspector]public string itemAssetName;
     [HideInInspector]public int soilIndex;
     [HideInInspector]public int slotIndex;
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
