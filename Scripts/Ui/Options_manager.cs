using System;
using UnityEngine;
using UnityEngine.UIElements;

public class Options_manager : MonoBehaviour
{
    public Dialogue_handler dialogue;
     Interaction current_interaction;
    public overworld_actions player;
    public Pokemon_party party;
    public Bag player_bag;
    public Player_Info_ui profile;
    public GameObject Menu_Options;
    public bool viewing_menu = false;
    public bool menu_off=true;
    public bool playerInBattle = false;
    public pokemon_storage storage;
    public Poke_Mart market;
    public Player_data player_data;
    public Obj_Instance ins_manager;
    public Recieve_Pokemon gift_pkm;
    public Game_Load game_state;
    public Item_handler item_h;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !player.using_ui && !player.doing_action &&!viewing_menu)
        {
            Menu_Options.SetActive(true);
            viewing_menu = true;
            player.using_ui = true;
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
            player.using_ui = false;
            player.movement.canmove = true;
        }
        if (Input.GetKeyDown(KeyCode.Escape) && player_bag.viewing_bag)
        {
            close_bag();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && profile.Viewing_profile)
        {
            profile.gameObject.SetActive(false);
            profile.Viewing_profile = false;
            player.using_ui = false;
            player.movement.canmove = true;
        }
        if (Input.GetKeyDown(KeyCode.Escape) && market.viewing_store)
        {
            Close_Store();
            dialogue.Write_Info("Would you like anything else?", "Options", "Buy_More","Sure, what would you like","Dont_Buy","Yes","No");
        }

        if (playerInBattle)
        {
            player.doing_action = true;
        }
    }
    //menu options
    public void Exit_To_menu()
    {
        dialogue.Write_Info("Are you sure you want to exit?, you will lose unsaved data!", "Options", "Exit_game","Good bye!","","Yes","No");
    }
    
    public void Close_Store()
    {
        market.Exit_Store();
        market.gameObject.SetActive(false);
        player.using_ui = false;
        player.movement.canmove = true;
    }

    public void close_bag()
    {
        player_bag.Close_bag();
        player_bag.gameObject.SetActive(false);
        player.using_ui = false;
        player.movement.canmove = true;
    }
    public void Menu_off()
    {
        menu_reset();
        player.using_ui = false;
        player.movement.canmove = true;
    }

    void menu_reset()
    {
        Menu_Options.SetActive(false);
        viewing_menu = false;
        menu_off = true;
    }
    public void View_Bag()
    {
        dialogue.Dialouge_off();
        player_bag.gameObject.SetActive(true);
        player.using_ui = true;
        player_bag.View_bag();
        menu_reset();
    }

    public void View_Profile()
    {
        player.using_ui = true;
        menu_reset();
        profile.gameObject.SetActive(true);
        profile.Load_Profile(player_data.Player_name, player_data.player_Money);
    }
    public void View_pkm_Party()
    {
        dialogue.Dialouge_off();
        party.party_ui.gameObject.SetActive(true);
        player.using_ui = true;
        party.View_party();
        menu_reset();
    }
    //option methods
    void Exit_game()
    {
        dialogue.Dialouge_off();
        game_state.Exit_game();
    }
    void Swim()
    {

    }
    void Battle()//trainer battle after interaction
    {

    }
    void Heal_Pokemon()
    {
        player.doing_action = true;
        dialogue.Write_Info(current_interaction.InterAction_result_msg, "Details");
        for (int i = 0; i < party.num_members; i++)
        {
            party.party[i].HP = party.party[i].max_HP;
            for (int j = 0; j < party.party[i].num_moves; j++)
            {
                party.party[i].move_set[j].Powerpoints = party.party[i].move_set[j].max_Powerpoints;
            }
        }
        player.doing_action = false;
        dialogue.Write_Info("Your pokemon have been healed, you're welcome!", "Details");
    }
    void PC_storage()
    {
        dialogue.Write_Info(current_interaction.InterAction_result_msg, "Details");
        storage.Open_pc();
        player.using_ui = true;
    }
    void View_Market()
    {
        dialogue.Write_Info(current_interaction.InterAction_result_msg, "Details");
        dialogue.Dialouge_off(1f);
        player.using_ui = true;
        Invoke(nameof(view_delay), 1f);
    }
    void view_delay()
    {
        market.gameObject.SetActive(true);
        market.View_store();
    }
    void Buy_More()
    {
        dialogue.Dialouge_off();
        View_Market();
    }
    void Dont_Buy()
    {
        dialogue.Write_Info("Have a great day!", "Details");
        dialogue.Dialouge_off(1f);
    }
    void Gift_pkm()
    {
        string pkm_name = current_interaction.InterAction_result_msg;
        Pokemon pkm = Resources.Load<Pokemon>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + pkm_name +"/"+ pkm_name);
        pkm.has_trainer = true;
        party.Add_Member(pkm);
        dialogue.Dialouge_off();
        dialogue.Write_Info("You got a " + pkm.Pokemon_name, "Details");
        gift_pkm.check_pkm(pkm_name);
    }
    void Interact()
    {
        dialogue.Write_Info(current_interaction.InterAction_result_msg, "Details");
        dialogue.Dialouge_off(2f);
    }
    void Fish()
    {
        player.doing_action = true;
        player.manager.change_animation_state(player.manager.Fishing_Start);
        dialogue.Write_Info(current_interaction.InterAction_result_msg, "Details");
    }
    void Sell_item()
    {
        dialogue.Dialouge_off();
        player_bag.Selling_items = true;
        View_Bag();
    }

    void Pick_Berry()
    {
        dialogue.Dialouge_off();
        string berry = current_interaction.InterAction_result_msg;
        Item bry = Resources.Load<Item>("Pokemon_project_assets/Player_obj/Bag/" + berry);
        player_bag.Add_item(ins_manager.set_Item(bry));
        dialogue.Write_Info("You picked up a "+berry, "Details");
        dialogue.Dialouge_off(1f);
    }
    public void Complete_Interaction(Interaction interaction,int option)
    {
        current_interaction = interaction;
        if (interaction.InterAction_type == "Options")
        {
            Invoke(interaction.InterAction_options[option], 0f);
        }
        if (interaction.InterAction_type == "List")
        {
            //list logic
            //dialogue.Write_Info("Would you like to fish for pokemon", "List", "fishing...",new string[]{"Fish","No fish","cool fish"},new string[]{"Yes","No","new"});
        }
    }

}
