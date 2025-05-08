using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collider_checks : MonoBehaviour
{
    public Area_manager area;
    [SerializeField] LayerMask Door;
    [SerializeField] Transform interaction_point;
    [SerializeField] float detect_distance = 0.15f;
    private void Start()
    {
        Door = 1 << LayerMask.NameToLayer("Door");
    }
    private void Update()
    {
        if (area.currentArea!=null)
        {
            if(area.currentArea.insideArea)
                Player_movement.instance.can_use_bike = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Encounter") && !Player_movement.instance.using_bike)
        {
            Player_movement.instance.can_use_bike = false;
        }

    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Encounter") && !Player_movement.instance.using_bike)
        {
            Player_movement.instance.can_use_bike = true;
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Encounter"))
        {
            Player_movement.instance.can_use_bike = false;
        }
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Switch_Area"))
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, interaction_point.forward, detect_distance, Door);
            if (hit.transform)
            {
                Switch_Area a = collision.transform.GetComponent<Switch_Area>();
                if (a.exitingArea)
                {
                    area.GoToOverworld();
                }
                else
                {
                    area.SwitchToArea(a,1f);
                }
            }
        }
    }
}
