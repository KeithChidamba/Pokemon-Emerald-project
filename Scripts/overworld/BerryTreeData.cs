using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "berryTree", menuName = "berryTree")]
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
     public int treeIndex;
     public int minutesPerStage;
     public bool loadedFromJson;
}
