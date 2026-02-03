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
    private Collider2D _collider2D;
    private bool _canTriggeredEncounter;
    public Tilemap tilemap;
    
    private void Start()
    {
        _collider2D = GetComponent<Collider2D>();
        _door = 1 << LayerMask.NameToLayer("Door");
        _canTriggeredEncounter = true;
        OnCollision += CheckGrass;
    }

    private void CheckGrass(Transform collisionTransform)
    {
        if (!collisionTransform.CompareTag("Encounter") || !_canTriggeredEncounter) return;
        
        Debug.Log("coll: "+collisionTransform.tag);
       
        var cellPos = tilemap.WorldToCell(transform.position + Vector3.down * 0.2f);

        Vector3 cellWorldCenter = tilemap.GetCellCenterWorld(cellPos);
        Debug.DrawLine(transform.position, cellWorldCenter, Color.red);
        
        Debug.Log("exists: "+tilemap.HasTile(cellPos));
        
        TileBase baseTile = tilemap.GetTile(cellPos);
        if (baseTile == null)
        {
            Debug.Log("Base tile: NULL");
            return;
        }

        Debug.Log("Base tile: " + baseTile.name);

        EncounterTile behaviorTile = baseTile as EncounterTile;
        if (behaviorTile == null)
        {
            Debug.Log("Base tile: NULL");
            return;
        }

        Debug.Log("Base tile: " + behaviorTile.name);

        return;
        
        var tile = tilemap.GetTile<EncounterTile>(cellPos);

        if (tile == null)
        {
            Debug.Log("no tile ");
            return;
        }
        
        _collider2D.isTrigger = true;
        
        if (Player_movement.Instance.usingBike)
            _collider2D.isTrigger = false;
        else
        {
            Player_movement.Instance.canUseBike = false;
            if (Player_movement.Instance.runningInput)
                Encounter_handler.Instance.overworldEncounterChance = 5;
            var randomNumber = Random.Range(1, 11);
            if (randomNumber <  Encounter_handler.Instance.overworldEncounterChance & ! Encounter_handler.Instance.encounterTriggered)
                Encounter_handler.Instance.TriggerEncounter( tile.area);
            _canTriggeredEncounter = false;
        }
    }
    private void OnCollisionEnter2D(Collision2D other)
    {
        OnCollision?.Invoke(other.transform);
        CheckDoor(other.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("trig ");
        OnCollision?.Invoke(other.transform);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Encounter")) return;
        _canTriggeredEncounter = true;
        Player_movement.Instance.canUseBike = true;
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
