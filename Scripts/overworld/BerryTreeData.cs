using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BerryTreeData : ScriptableObject
{
     public int minYield;
     public int maxYield;
     public bool currentStageNeedsWater;
     public int currentStageProgress;
     public float minutesSinceLastStage;
     public TimeSpan timeOfLastLogin;
     public string itemAssetName;
     public Item berryItem;
     public int minutesPerStage;
}
