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

//deprecated until further notice
    // public Vector3 GetTeleportWorldPosition(Tilemap doorTileMap)
    // {
    //     Vector3 pos = doorTileMap.GetCellCenterWorld(entranceCell);
    //     return Vector3Int.RoundToInt(pos);
    // }

}
public enum AreaName
{
    OverWorld,PlayerGarden,PokeMartCoastal,PokeCenter,SouthBridge
}