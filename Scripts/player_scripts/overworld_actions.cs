using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class overworld_actions : MonoBehaviour
{
    public Animation_manager manager;
    float bike_speed = 2f;
    public bool canSwitch = false;
    public bool fishing = false;
    [SerializeField] private bool PokemonBiting = false;
    public bool doing_action = false;
    public bool using_ui = false;
    public Encounter_Area fishingArea;
    public static overworld_actions instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    void Update()
    {
        if (!using_ui)
        {
            if (doing_action)
            {
                Player_movement.instance.canmove = false;
                canSwitch = false;
            }

            if (PokemonBiting & Input.GetKeyDown(KeyCode.F))
            {
                PokemonBiting = false;
                Encounter_handler.instance.Trigger_encounter(fishingArea);
            }
            if (fishing)
            {
                doing_action = true;
                manager.change_animation_state(manager.Fishing_idle);
                if (Input.GetKeyDown(KeyCode.Q))
                    Done_fishing();
            }
        }
        else
            Player_movement.instance.canmove = false;
    }

    IEnumerator TryFishing()
    {
        int random = Utility.RandomRange(1, 11);
        yield return new WaitForSeconds(1f);
        if (!fishing) yield break;
        if (random< 5)
        {
            PokemonBiting = true;
            Dialogue_handler.instance.Write_Info("Oh!, a Bite!, Press F","Details");
            yield return new WaitForSeconds( (2 * (random/10) ) + 1f);
            if (PokemonBiting)
            {
                Dialogue_handler.instance.Write_Info("It got away","Details");
                Done_fishing();
            }
        }
        else
        {
            Dialogue_handler.instance.Write_Info("Dang...nothing","Details");
            Done_fishing();
        }
    }
    void Start_fishing()
    {
        fishing = true;
        StartCoroutine(TryFishing());
    }
    public void Done_fishing()
    {
        fishing = false;
        PokemonBiting = false;
        Invoke(nameof(Action_reset), 0.8f);
        manager.change_animation_state(manager.Fishing_End);
        Dialogue_handler.instance.Dialouge_off();
    }
    void Action_reset()
    {
        doing_action = false;
        Player_movement.instance.canmove = true;
    }
    public void Use_Bike()
    {
        Player_movement.instance.movement_speed = bike_speed;
    }
}
