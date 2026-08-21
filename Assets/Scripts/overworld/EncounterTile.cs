using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/Encounter Tile")]
public class EncounterTile : Tile
{
    public EncounterTable table;
}
