using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game_ui_manager : MonoBehaviour
{

    public GameObject Menu_Options;
    public bool viewing_menu = false;
    public bool menu_off=true;
    public Player_Info_ui profile;
    public static Game_ui_manager instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    private void Update()
    {
        if (overworld_actions.instance == null) return;
            Ui_inputs();
    }
    private void Ui_inputs()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !overworld_actions.instance.using_ui && !overworld_actions.instance.doing_action &&!viewing_menu)
        {
            viewing_menu = true;
            Use_ui(Menu_Options);
        }
        if (Input.GetKeyUp(KeyCode.Space) && !overworld_actions.instance.doing_action && viewing_menu)
        {
            menu_off = false;
        }
        if (Input.GetKeyDown(KeyCode.Space) && viewing_menu && !menu_off)
        {
            Menu_off();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && Pokemon_party.instance.viewing_party && !Pokemon_party.instance.viewing_details)
        {
            if(!Pokemon_party.instance.SwapOutNext)
                Close_party();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && Bag.instance.viewing_bag)
        {
            close_bag();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && profile.Viewing_profile)
        {
            profile.gameObject.SetActive(false);
            profile.Viewing_profile = false;
            Reset_player_movement();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && Poke_Mart.instance.viewing_store)
        {
            Close_Store();
            Dialogue_handler.instance.Write_Info("Would you like anything else?", "Options", "Buy_More","Sure, what would you like","Dont_Buy","Yes","No");
        }
    } 
    public void Reset_player_movement()
    {
        overworld_actions.instance.using_ui = false;
        Player_movement.instance.canmove = true;
    }
    void Use_ui(GameObject ui)
    {
        ui.SetActive(true);
        overworld_actions.instance.using_ui = true;
    }
    public void Close_Store()
    {
        Poke_Mart.instance.Exit_Store();
        Poke_Mart.instance.mart_ui.SetActive(false);
        Reset_player_movement();
    }
    public void close_bag()
    {
        Bag.instance.Close_bag();
        Bag.instance.bag_ui.SetActive(false);
        if (!Options_manager.instance.playerInBattle)
        {
            Reset_player_movement();
        }
        else
        {
            overworld_actions.instance.using_ui = false;
        }
    }
    public void Close_party()
    {
        Pokemon_party.instance.party_ui.gameObject.SetActive(false);
        Pokemon_party.instance.Cancel();
        if (Options_manager.instance.playerInBattle)
            overworld_actions.instance.using_ui = false;
        else
            Reset_player_movement();
    }
    public void Menu_off()
    {
        menu_reset();
        Reset_player_movement();
    }
    void menu_reset()
    {
        Menu_Options.SetActive(false);
        viewing_menu = false;
        menu_off = true;
    }
    public void view_market()
    {
        Use_ui(Poke_Mart.instance.mart_ui);
        Poke_Mart.instance.View_store();
    }
    public void View_Bag()
    {
        Dialogue_handler.instance.Dialouge_off();
        Use_ui(Bag.instance.bag_ui);
        Bag.instance.View_bag();
        menu_reset();
    }
    public void View_Profile()
    {
        Use_ui(profile.gameObject);
        menu_reset();
        profile.Load_Profile(Game_Load.instance.player_data);
    }
    public void View_pkm_Party()
    {
        menu_reset();
        Dialogue_handler.instance.Dialouge_off();
        Use_ui(Pokemon_party.instance.party_ui);
        Pokemon_party.instance.View_party();
    }
}
