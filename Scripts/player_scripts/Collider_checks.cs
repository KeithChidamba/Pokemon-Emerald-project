using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class Collider_checks : MonoBehaviour
{
    public Area_manager area;
    private LayerMask _door;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float detectionDistance = 0.5f;
    public static event Action<Transform> OnCollision;
    public Tilemap tilemap;
    
    private void Start()
    {
        _door = 1 << LayerMask.NameToLayer("Door");
        Player_movement.Instance.OnNewTile += CheckGrass;
    }

    private void CheckGrass()
    {
        var cellPos = tilemap.WorldToCell(transform.position + Vector3.down * 0.2f);
        var tile = tilemap.GetTile<EncounterTile>(cellPos);

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
        CheckDoor(other.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        OnCollision?.Invoke(other.transform);
    }

    private void CheckDoor(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Switch_Area")) return;
        
        var hit = Physics2D.Raycast(transform.position, interactionPoint.forward, detectionDistance, _door);
        if (!hit.transform) return; 
        var areaEntryPoint = collision.transform.GetComponent<Switch_Area>();
        if (areaEntryPoint.areaData.exitingArea)
            area.GoToOverworld();
        else
            area.EnterBuilding(areaEntryPoint,1f);
    }

}
