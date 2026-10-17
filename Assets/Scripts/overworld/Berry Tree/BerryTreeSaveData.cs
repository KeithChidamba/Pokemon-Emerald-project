using System;
using UnityEngine;
[Serializable]
public class BerryTreeSaveData
{
    public bool isPlanted;
    public bool currentStageNeedsWater;
    public int currentStageProgress;
    public float minutesSinceLastStage;
    public int numStagesWatered;
    public string itemAssetName;
    public int soilIndex;
    public string lastLogin;

    public void SaveData(BerryTreeData data)
    {
        isPlanted = data.isPlanted;
        currentStageNeedsWater = data.currentStageNeedsWater;
        currentStageProgress = data.currentStageProgress;
        minutesSinceLastStage = data.minutesSinceLastStage;
        numStagesWatered = data.numStagesWatered;
        itemAssetName = data.itemAssetName;
        soilIndex = data.soilIndex;
        lastLogin = DateTime.Now.ToString("o"); // "o" = ISO 8601
    }

    public BerryTreeData LoadTree()
    {
        var treeAsset = Resources.Load<BerryTreeData>(DirectoryHandler.GetDirectory(AssetDirectory.BerryTreeData)
                                                      + $"{itemAssetName} Data");
        
        var treeData = ScriptableObject.CreateInstance<BerryTreeData>();
        
        treeData.berryItem = treeAsset.berryItem;
        treeData.spriteData = treeAsset.spriteData;
        treeData.minYield = treeAsset.minYield;
        treeData.maxYield = treeAsset.maxYield;
        treeData.minutesPerStage = treeAsset.minutesPerStage;
        
        treeData.isPlanted = isPlanted;
        treeData.currentStageNeedsWater = currentStageNeedsWater;
        treeData.currentStageProgress = currentStageProgress;
        treeData.minutesSinceLastStage = minutesSinceLastStage;
        treeData.numStagesWatered = numStagesWatered;
        treeData.itemAssetName = itemAssetName;
        treeData.soilIndex = soilIndex;
        treeData.lastLogin = lastLogin;
        
        return treeData;
    }
}
