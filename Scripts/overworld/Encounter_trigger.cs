using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Encounter_trigger : MonoBehaviour
{
    public Encounter_handler handler;
    public Encounter_Area area;
    private bool _triggeredEncounter;
    private Collider2D _triggerCheckCollider;
    private void Start()
    {
        _triggerCheckCollider = gameObject.GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player") || _triggeredEncounter) return;
        var player = collision.GetComponentInParent<Player_movement>();
        if (player.using_bike)
            _triggerCheckCollider.isTrigger = false;
        else
        {
            if (player.running)
                handler.encounterChance = 5;
            var randomNumber = Random.Range(1, 11);
            if (randomNumber < handler.encounterChance & !handler.encounterTriggered)
                handler.Trigger_encounter(area);
            _triggeredEncounter = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
            Invoke(nameof(ResetTrigger),2f);
    }

    void ResetTrigger()
    {
        _triggeredEncounter = false;
    }
    private void CheckCollision(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        var player = collision.transform.GetComponentInParent<Player_movement>();
        if (!player.using_bike)
            gameObject.GetComponent<Collider2D>().isTrigger = true;
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        CheckCollision(collision);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckCollision(collision);
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckCollision(collision);
    }
}
