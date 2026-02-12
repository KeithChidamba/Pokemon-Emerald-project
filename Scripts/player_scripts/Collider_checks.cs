using System;
using System.Collections.Generic;
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
    
    private void CheckGrass()
    {
        var tile = FindTileAtPosition<EncounterTile>(encounterTilemap,transform.position,Vector3.down);
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
    public static T FindTileAtPosition<T>(Tilemap tilemap,Vector3 triggerPos,Vector3 offset) where T : TileBase
    {
        var worldPos = triggerPos + offset * 0.01f;
        var cellPos = tilemap.WorldToCell(worldPos);
        return tilemap.GetTile<T>(cellPos);
    }
    public static T FindTileAtPositionRadius<T>(Tilemap tilemap,Vector3 triggerPos,Vector3 offset) where T : TileBase
    {//works for 1x1 scale tile system
        List<Vector3> radius_3x3_Matrix = new()
        {
            new(triggerPos.x, triggerPos.y + 1, 0), //up
            new(triggerPos.x, triggerPos.y - 1, 0), //down

            new(triggerPos.x + 1, triggerPos.y, 0), //left
            new(triggerPos.x - 1, triggerPos.y, 0), //right

            new(triggerPos.x + 1, triggerPos.y + 1, 0), //top left
            new(triggerPos.x - 1, triggerPos.y + 1, 0), //top right

            new(triggerPos.x + 1, triggerPos.y - 1, 0), //bottom left
            new(triggerPos.x - 1, triggerPos.y - 1, 0), //bottom right
        };
       foreach(var position in radius_3x3_Matrix)
        {
            var tile = FindTileAtPosition<T>(tilemap, position, offset);
            if (tile != null)
            {
                return tile;
            }
        }
        return null;
    }
}

