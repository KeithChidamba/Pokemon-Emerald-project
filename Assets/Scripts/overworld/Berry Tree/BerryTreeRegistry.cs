using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "berryTree Registry", menuName = "Overworld/Berry tree registry")]
public class BerryTreeRegistry : ScriptableObject
{
    [FormerlySerializedAs("berryTrees")] public List<OverworldTree> soilGroups;
}

[Serializable]
public class OverworldTree
{
    public Vector3 treePosition;
    public List<BerryTreeData> treeData;
    public bool loadedFromJson;
    public int numTreesLoaded;
}

