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
        if (Player_movement.Instance.usingBike)
            _triggerCheckCollider.isTrigger = false;
        else
        {
            Player_movement.Instance.canUseBike = false;
            if (Player_movement.Instance.runningInput)
                handler.overworldEncounterChance = 5;
            var randomNumber = Random.Range(1, 11);
            if (randomNumber < handler.overworldEncounterChance & !handler.encounterTriggered)
                handler.TriggerEncounter(area);
            _triggeredEncounter = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Player_movement.Instance.canUseBike = true;
            Invoke(nameof(ResetTrigger),2f);
        }
    }

    void ResetTrigger()
    {
        _triggeredEncounter = false;
    }
}
