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
    public List<StoryObjective> currentStoryObjectives = new();

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
        var trees = FindObjectsOfType<BerryTree>();
        foreach(var tree in trees)
        {
            overworldBerryTrees.Add(tree);
        }
        Save_manager.Instance.LoadOverworldData();
        foreach (var treeData in treeDataQueue)
        {
            var jsonBerryTree = overworldBerryTrees.First(tree=>tree.treeIndex==treeData.treeIndex);
            jsonBerryTree.loadedFromJson = true;
            jsonBerryTree.LoadTreeData(treeData);
        }
        foreach (var tree in overworldBerryTrees)
        {
            if(tree.loadedFromJson)continue;
            tree.LoadDefaultAsset();
        }

        if (currentStoryObjectives.Count > 0)
        {
            Game_Load.Instance.OnGameStarted += LoadStory;
        }
    }

    void LoadStory()
    {
        currentStoryObjectives[0].LoadObjective();
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
        yield return null;
    }
}
