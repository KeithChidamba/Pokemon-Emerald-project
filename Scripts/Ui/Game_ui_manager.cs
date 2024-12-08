using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game_ui_manager : MonoBehaviour
{
    public Options_manager options;
    public GameObject Menu_Options;
    public bool viewing_menu = false;
    public bool menu_off=true;
    public overworld_actions player;
    public Pokemon_party party;
    public Bag player_bag;
    public Battle_handler battle;
    public Player_Info_ui profile;
    public Poke_Mart market;
    public Dialogue_handler dialogue;
    private void Update()
    {
        Ui_inputs();
    }
    private void Ui_inputs()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !player.using_ui && !player.doing_action &&!viewing_menu)
        {
            viewing_menu = true;
            Use_ui(Menu_Options);
        }
        if (Input.GetKeyUp(KeyCode.Space) && !player.doing_action && viewing_menu)
        {
            menu_off = false;
        }
        if (Input.GetKeyDown(KeyCode.Space) && viewing_menu && !menu_off)
        {
            Menu_off();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && party.viewing_party && !party.viewing_details)
        {
            party.party_ui.gameObject.SetActive(false);
            if (options.playerInBattle)
            {
                battle.Set_pkm();
                player.using_ui = false;
            }
            else
            {
                Reset_player_movement();
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape) && player_bag.viewing_bag)
        {
            close_bag();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && profile.Viewing_profile)
        {
            profile.gameObject.SetActive(false);
            profile.Viewing_profile = false;
            Reset_player_movement();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && market.viewing_store)
        {
            Close_Store();
            dialogue.Write_Info("Would you like anything else?", "Options", "Buy_More","Sure, what would you like","Dont_Buy","Yes","No");
        }
    }
    void Reset_player_movement()
    {
        player.using_ui = false;
        player.movement.canmove = true;
    }
    void Use_ui(GameObject ui)
    {
        ui.SetActive(true);
        player.using_ui = true;
    }
    public void Close_Store()
    {
        market.Exit_Store();
        market.gameObject.SetActive(false);
        Reset_player_movement();
    }
    public void close_bag()
    {
        player_bag.Close_bag();
        player_bag.gameObject.SetActive(false);
        if (!options.playerInBattle)
        {
            Reset_player_movement();
        }
        else
        {
            player.using_ui = false;
        }
            
        if (options.playerInBattle)
        {
            battle.Set_pkm();
        }
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
        Use_ui(market.gameObject);
        market.View_store();
    }
    public void View_Bag()
    {
        dialogue.Dialouge_off();
        Use_ui(player_bag.gameObject);
        player_bag.View_bag();
        menu_reset();
    }
    public void View_Profile()
    {
        Use_ui(profile.gameObject);
        menu_reset();
        profile.Load_Profile(options.player_data.Player_name, options.player_data.player_Money);
    }
    public void View_pkm_Party()
    {
        menu_reset();
        dialogue.Dialouge_off();
        Use_ui(party.party_ui);
        party.View_party();
    }
}
