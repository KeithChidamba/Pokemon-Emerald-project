using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class Collider_checks : MonoBehaviour
{
    [SerializeField] private Transform interactionPoint;
    public static event Action<Transform> OnCollision;
    public Tilemap encounterTilemap;
    private void Start()
    {
        Player_movement.Instance.OnNewTile += CheckGrass;
    }
    
    private T FindTileAtPosition<T>(Tilemap tilemap) where T : Tile
    {
        var worldPos = Vector3Int.RoundToInt(transform.position + Vector3.down * 0.3f);
        var cellPos = tilemap.WorldToCell(worldPos);
        return tilemap.GetTile<T>(cellPos);
    }
    private void CheckGrass()
    {
        var tile = FindTileAtPosition<EncounterTile>(encounterTilemap);
        if (tile == null) return;
        
        if (Player_movement.Instance.runningInput) Encounter_handler.Instance.overworldEncounterChance = 5;
        
        var randomNumber = Random.Range(1, 11);
        
        if (randomNumber < Encounter_handler.Instance.overworldEncounterChance &
            !Encounter_handler.Instance.encounterTriggered)
        {
            Encounter_handler.Instance.TriggerEncounter(tile.area);
        }

    }
    private void OnCollisionEnter2D(Collision2D other)
    {
        OnCollision?.Invoke(other.transform);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        OnCollision?.Invoke(other.transform);
    }

}
