using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Encounter_trigger : MonoBehaviour
{
    public Encounter_handler handler;
    [SerializeField] bool triggered = false;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !triggered)
        {
            Player_movement player = collision.GetComponentInParent<Player_movement>();
            if (player.using_bike)
            {
                gameObject.GetComponent<Collider2D>().isTrigger = false;
            }
            else
            {
                if (player.running)
                {
                    handler.encounter_chance = 5;
                }
                int rand = Random.Range(1, 11);
                if (rand < handler.encounter_chance && !triggered)
                {
                    if (!handler.triggered_encounter)
                    {
                        handler.Trigger_encounter();
                    }
                }
                triggered = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
            Invoke(nameof(Reset_trigger),2f);
    }

    void Reset_trigger()
    {
        triggered = false;
    }
    void col(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Player_movement player = collision.transform.GetComponentInParent<Player_movement>();
            if (!player.using_bike)
            {
                gameObject.GetComponent<Collider2D>().isTrigger = true;
            }
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        col(collision);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        col(collision);
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        col(collision);
    }
}
