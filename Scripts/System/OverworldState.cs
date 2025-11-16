using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OverworldState : MonoBehaviour
{    
    [SerializeField]private bool treesLoaded;
    [SerializeField]private List<BerryTree> overworldBerryTrees = new();
    [SerializeField]private List<BerryTreeData> treeDataQueue = new();
    public static OverworldState Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        treesLoaded = false;
        Save_manager.Instance.OnOverworldDataLoaded += ()=> StartCoroutine(LoadAllTreeData());
    }
    private void Start()
    {
        overworldBerryTrees.Clear();
        var trees = FindObjectsOfType<BerryTree>();
        foreach(var tree in trees)
        {
            overworldBerryTrees.Add(tree);
        }
        treesLoaded = true;
        Save_manager.Instance.OnPlayerDataSaved += SaveOverworldData;
    }
    public int GetTreeIndex(BerryTree tree)
    {
        return overworldBerryTrees.IndexOf(tree);
    }
    public void StoreBerryTreeData(BerryTreeData treeData)
    {
        treeDataQueue.Add(treeData);
    }
    private IEnumerator LoadAllTreeData()
    {
        yield return new WaitUntil(() => treesLoaded);
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
    }
    private IEnumerator SaveOverworldData()
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
