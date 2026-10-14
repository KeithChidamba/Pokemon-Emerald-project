using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Overworld/Area Transition Data")]
public class AreaData : ScriptableObject
{
    public AreaName areaName;
    public Vector3 entranceCell;
    public Biome biome;
    public List<OverwolrdNpcData> npcList = new();
}

[Serializable]
public class OverwolrdNpcData
{
    public NpcCreationData npcData;
    public bool autoDetectPlayer;
    public bool resumeMovementAfterPause;
    public float detectDistance = 1f;
}