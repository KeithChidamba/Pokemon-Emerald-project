using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OverworldState : MonoBehaviour
{
    public List<BerryTree> overworldBerryTrees = new();
    public static OverworldState Instance;
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
        Save_manager.Instance.OnPlayerDataSaved += SaveOverworldData;
    }

    public int GetTreeIndex(BerryTree tree)
    {
        return overworldBerryTrees.IndexOf(tree);
    }
    public void LoadBerryTreeData(BerryTreeData treeData)
    {
        var currentTree = overworldBerryTrees[treeData.treeIndex];
        currentTree.OnTreeAwake += () => currentTree.LoadTreeData(treeData);
    }
    private IEnumerator SaveOverworldData()
    {
        foreach (var tree in overworldBerryTrees)
        {
            tree.treeData.lastLogin = DateTime.Now.ToString("o");
            
            Save_manager.Instance
                .SaveBerryTreeDataAsJson(tree.treeData,"BerryTree: "+ tree.treeData.treeIndex);
        }
        yield return null;
    }
}
