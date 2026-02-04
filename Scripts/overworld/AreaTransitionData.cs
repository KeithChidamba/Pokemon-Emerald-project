using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Overworld/Area Transition Data")]
public class AreaTransitionData : ScriptableObject
{
    public Tilemap tilemap;
    public GameObject overworld;
    public GameObject interior;
    public AreaData areaData;

    public Vector3Int doormatCell;
    public Vector3Int doorCell;

    public Vector3 GetDoormatWorldPosition()
    {
        return tilemap.GetCellCenterWorld(doormatCell);
    }

    public Vector3 GetDoorWorldPosition()
    {
        return tilemap.GetCellCenterWorld(doorCell);
    }
}
