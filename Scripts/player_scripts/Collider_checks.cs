using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Collider_checks : MonoBehaviour
{
    public Area_manager area;
    private LayerMask _door;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float detectionDistance = 0.15f;
    private void Start()
    {
        _door = 1 << LayerMask.NameToLayer("Door");
    }
    private void Update()
    {
        if (area.currentArea == null) return;
        if(area.currentArea.insideArea)
            Player_movement.instance.can_use_bike = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Encounter") && !Player_movement.instance.using_bike)
            Player_movement.instance.can_use_bike = false;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Encounter") && !Player_movement.instance.using_bike)
            Player_movement.instance.can_use_bike = true;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Encounter"))
            Player_movement.instance.can_use_bike = false;
    }
    private void OnCollisionStay2D(Collision2D collision)
    { 
        if (!collision.gameObject.CompareTag("Switch_Area")) return;
        var hit = Physics2D.Raycast(transform.position, interactionPoint.forward, detectionDistance, _door);
        if (!hit.transform) return; 
            var areaEntryPoint = collision.transform.GetComponent<Switch_Area>();
            if (areaEntryPoint.exitingArea)
                area.GoToOverworld();
            else
                area.SwitchToArea(areaEntryPoint,1f);
        
    }
}
