using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Overworld/Area Transition Data")]
public class AreaTransitionData : ScriptableObject
{
    public AreaName areaName;
    public bool escapable;
    public bool isBuidlingEntrance;
    public Vector3 entranceCell;
    public Vector3 exitCell;
    public AreaName overworldAreaName;
    public Biome biome;
}
