using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OverworldState : MonoBehaviour
{    
    [SerializeField]private List<BerryTree> overworldBerryTrees = new();
    [SerializeField]private List<BerryTreeData> treeDataQueue = new();
    public static OverworldState Instance;
    public List<StoryObjective> allStoryObjectives = new();
    public List<StoryObjective> currentStoryObjectives = new();
    public StoryProgressObjective storyProgressObjective;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        overworldBerryTrees.Clear();
        currentStoryObjectives.Clear();
        
        var trees = FindObjectsOfType<BerryTree>();
        foreach(var tree in trees)
        {
            overworldBerryTrees.Add(tree);
        }
        Save_manager.Instance.LoadOverworldData();
        
        foreach (var treeData in treeDataQueue)
        {
            var jsonBerryTree = overworldBerryTrees.First(tree=>tree.treeIndex==treeData.treeIndex);
            jsonBerryTree.name = treeData.itemAssetName + " Tree";
            jsonBerryTree.loadedFromJson = true;
            jsonBerryTree.LoadTreeData(treeData);
        }
        foreach (var tree in overworldBerryTrees)
        {
            if(tree.loadedFromJson)continue;
            tree.LoadDefaultAsset();
        }
        
        if (storyProgressObjective == null)
        {
            currentStoryObjectives.AddRange(allStoryObjectives); 
            currentStoryObjectives.ForEach(o=>o.mainAssetName=o.name);
            
            storyProgressObjective = Resources.Load<StoryProgressObjective>(Save_manager.GetDirectory(AssetDirectory.StoryObjectiveData)+"Story Progress");
            storyProgressObjective.mainAssetName = storyProgressObjective.name;
            storyProgressObjective.totalObjectiveAmount = allStoryObjectives.Count;
            storyProgressObjective.numCompleted = 0;
        }
        else
        {
            var orderList = currentStoryObjectives.OrderBy(obj => obj.indexInList).ToList();
            currentStoryObjectives.Clear();
            currentStoryObjectives.AddRange(orderList);
        }
        
        if (storyProgressObjective.numCompleted < storyProgressObjective.totalObjectiveAmount)
        {
            Game_Load.Instance.OnGameStarted += ()=>currentStoryObjectives[0].FindMainAsset();
        }
    }

    public void ClearAndLoadNextObjective()
    {
        currentStoryObjectives.RemoveAt(0);
        storyProgressObjective.numCompleted++;
        if (currentStoryObjectives.Count > 0)
        {
            currentStoryObjectives[0].FindMainAsset();
        }
        else
        {
            Dialogue_handler.Instance.RemoveObjectiveText();
        }
    }
    public int GetTreeIndex(BerryTree tree)
    {
        return overworldBerryTrees.IndexOf(tree);
    }
    public void StoreBerryTreeData(BerryTreeData treeData)
    {
        treeDataQueue.Add(treeData);
    }
    public IEnumerator SaveOverworldData()
    {
        foreach (var tree in overworldBerryTrees)
        {
            tree.treeData.SetLastLogin(DateTime.Now);
            Save_manager.Instance
                .SaveBerryTreeDataAsJson(tree.treeData,"BerryTree "+ tree.treeData.treeIndex);
        }
        yield return new WaitForSeconds(1f);
        int objectiveIndex=0;
        foreach (var objective in currentStoryObjectives)
        {
            objective.mainAssetName = objective.mainAssetName==string.Empty? objective.name:objective.mainAssetName;
            objective.indexInList = objectiveIndex;
            objectiveIndex++;
            Save_manager.Instance.SaveStoryDataAsJson(objective,objective.objectiveHeading);
            yield return new WaitForSeconds(0.025f);
        }
        storyProgressObjective.mainAssetName = storyProgressObjective.mainAssetName==string.Empty? storyProgressObjective.name:storyProgressObjective.mainAssetName;
        Save_manager.Instance.SaveStoryDataAsJson(storyProgressObjective,"Story Progress");
        yield return null;
    }
}
