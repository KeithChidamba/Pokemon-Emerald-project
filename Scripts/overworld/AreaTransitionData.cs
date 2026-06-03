using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Overworld/Area Transition Data")]
public class AreaTransitionData : ScriptableObject
{
    public AreaName areaName;
    public bool escapable;
    public Vector3Int entranceCell;
    public Biome biome;
}
public enum AreaName
{
    OverWorld,PlayerGarden,PokeMartCoastal,PokeCenter,SouthBridge
}
