using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Overworld/Area Transition Data")]
public class AreaTransitionData : ScriptableObject
{
    public AreaData areaData;

    public Vector3Int doormatCell;
    public Vector3Int doorCell;

    public Vector3 GetTeleportWorldPosition(Tilemap doorTileMap)
    {
        return doorTileMap.GetCellCenterWorld(areaData.insideArea?doorCell:doormatCell);
    }

}
