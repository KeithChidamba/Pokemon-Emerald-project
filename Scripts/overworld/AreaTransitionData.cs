using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Overworld/Area Transition Data")]
public class AreaTransitionData : ScriptableObject
{
    public AreaData areaData;
    public Vector3Int doormatCell;


    public Vector3 GetTeleportWorldPosition(Tilemap doorTileMap)
    {
        Vector3 pos = doorTileMap.GetCellCenterWorld(doormatCell);
        return Vector3Int.RoundToInt(pos);
    }

}
