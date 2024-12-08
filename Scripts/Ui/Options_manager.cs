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
    public bool playerInBattle = false;
    public pokemon_storage storage;
    public Poke_Mart market;
    public Player_data player_data;
    public Obj_Instance ins_manager;
    public Recieve_Pokemon gift_pkm;
    public Game_Load game_state;
    public Battle_handler battle;
    public Item_handler item_h;
    public Game_ui_manager ui_m;
    public Utility util;
    private void Update()
    {
        if (playerInBattle)
        {
            player.doing_action = true;
        }
    }
    //dialogue options
    void Exit_game()
    {
        dialogue.Dialouge_off();
        game_state.Exit_game();
    }
    public void Exit_To_menu()
    {
        dialogue.Write_Info("Are you sure you want to exit?, you will lose unsaved data!", "Options", "Exit_game","Good bye!","","Yes","No");
    }
    void Swim()//will make later
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
    void Buy_More()
    {
        dialogue.Dialouge_off();
        dialogue.Write_Info(current_interaction.InterAction_result_msg, "Details");
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
    void Interact()//give player info about what they interacted with
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
        ui_m.View_Bag();
    }
    void View_Market()
    {
        dialogue.Dialouge_off(0.4f);
        ui_m.view_market();
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
            //list logic, might be useful later 
            //dialogue.Write_Info("Would you like to fish for pokemon", "List", "fishing...",new string[]{"Fish","No fish","cool fish"},new string[]{"Yes","No","new"});
        }
    }

}
