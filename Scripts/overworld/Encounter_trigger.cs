using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Encounter_trigger : MonoBehaviour
{
    public Encounter_handler handler;
    bool triggered = false;
    private void OnTriggerStay2D(Collider2D collision)
    {
        int encounter_chance = 20;
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
                    encounter_chance = 40;
                }
                int rand = Random.Range(1, 101);
                if (rand < encounter_chance)
                {
                    if (!handler.triggered_encounter)
                    {
                        handler.Trigger_encounter();
                    }
                    else
                    {
                        triggered = true;
                        Invoke(nameof(Reset_trigger),2f);
                    }
                }
            }
        }
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
