using System;
using UnityEngine;
using UnityEngine.UIElements;

public class Options_manager : MonoBehaviour
{
    private Interaction current_interaction;
    public bool playerInBattle = false;
    public bool ChoosingNewMove = false;
    [SerializeField] private Recieve_Pokemon gift_pkm;
    public static Options_manager instance;
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
        if (playerInBattle)
            overworld_actions.instance.doing_action = true;
    }
    void Exit_game()
    {
        Dialogue_handler.instance.Dialouge_off();
        Game_Load.instance.Exit_game();
    }
    public void Exit_To_menu()
    {
        Dialogue_handler.instance.Write_Info("Are you sure you want to exit?, you will lose unsaved data!", "Options", "Exit_game","Good bye!","","Yes","No");
    }
    void Swim()//will make later
    {

    }
    void Battle()//trainer battle after interaction
    {

    }

    void Learn_Move()
    {
        Pokemon_Details.instance.LearningMove = true;
        Pokemon_Details.instance.Load_Details(PokemonOperations.CurrentPkm);
        Dialogue_handler.instance.Battle_Info("Which move will you replace?");
        ChoosingNewMove = false;
    }
    void Skip_Move()
    {
        ChoosingNewMove = false;
        Pokemon_Details.instance.LearningMove = false;
        PokemonOperations.LearningNewMove = false;
        Dialogue_handler.instance.Battle_Info(PokemonOperations.CurrentPkm.Pokemon_name+" did not learn "+PokemonOperations.NewMove.Move_name);
        Battle_handler.instance.levelUpQueue.RemoveAll(p=>p.pokemon==PokemonOperations.CurrentPkm);
    }
    void Heal_Pokemon()
    {
        overworld_actions.instance.doing_action = true;
        Dialogue_handler.instance.Write_Info(current_interaction.ResultMessage, "Details");
        for (int i = 0; i < Pokemon_party.instance.num_members; i++)
        {
            Pokemon_party.instance.party[i].HP = Pokemon_party.instance.party[i].max_HP;
            foreach (Move m in Pokemon_party.instance.party[i].move_set)
                m.Powerpoints = m.max_Powerpoints;
        }
        overworld_actions.instance.doing_action = false;
        Dialogue_handler.instance.Write_Info("Your pokemon have been healed, you're welcome!", "Details");
    }
    void PC_storage()
    {
        Dialogue_handler.instance.Write_Info(current_interaction.ResultMessage, "Details");
        pokemon_storage.instance.Open_pc();
        overworld_actions.instance.using_ui = true;
    }
    void Buy_More()
    {
        Dialogue_handler.instance.Dialouge_off();
        Dialogue_handler.instance.Write_Info(current_interaction.ResultMessage, "Details");
        ViewMarketDelayed();
    }
    void Dont_Buy()
    {
        Dialogue_handler.instance.Write_Info("Have a great day!", "Details");
        Dialogue_handler.instance.Dialouge_off(1f);
    }
    void Gift_pkm()
    {
        string pkm_name = current_interaction.ResultMessage;
        Pokemon pkm = Resources.Load<Pokemon>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + pkm_name +"/"+ pkm_name);
        Pokemon_party.instance.Add_Member(pkm);
        Dialogue_handler.instance.Dialouge_off();
        Dialogue_handler.instance.Write_Info("You got a " + pkm.Pokemon_name, "Details");
        gift_pkm.check_pkm(pkm_name);
    }
    void Interact()
    {
        Dialogue_handler.instance.Write_Info(current_interaction.ResultMessage, "Details");
        Dialogue_handler.instance.Dialouge_off(2f);
    }
    void Fish()
    {
        overworld_actions.instance.doing_action = true;
        overworld_actions.instance.manager.change_animation_state(overworld_actions.instance.manager.Fishing_Start);
        Dialogue_handler.instance.Write_Info(current_interaction.ResultMessage, "Details");
    }
    void Sell_item()
    {
        Dialogue_handler.instance.Dialouge_off();
        Bag.instance.Selling_items = true;
        Game_ui_manager.instance.View_Bag();
    }
    void ViewMarketDelayed()//used in interaction as well
    {
        Invoke(nameof(View_Market),1f);
    }
    void View_Market()
    {
        Dialogue_handler.instance.Dialouge_off();
        Game_ui_manager.instance.view_market();
    }
    void Pick_Berry()
    {
        Dialogue_handler.instance.Dialouge_off();
        string berry = current_interaction.ResultMessage;
        Item bry = Resources.Load<Item>("Pokemon_project_assets/Player_obj/Bag/" + berry);
        Bag.instance.Add_item(Obj_Instance.set_Item(bry));
        Dialogue_handler.instance.Write_Info("You picked up a "+berry, "Details");
        Dialogue_handler.instance.Dialouge_off(1f);
    }
    public void Complete_Interaction(Interaction interaction,int option)
    {
        current_interaction = interaction;
        if (interaction.InteractionType == "Options")
            Invoke(interaction.InteractionOptions[option], 0f);
        if (interaction.InteractionType == "List")
        {
            //list logic, might be useful later 
            //Dialogue_handler.instance.Write_Info("Would you like to fish for pokemon", "List", "fishing...",new string[]{"Fish","No fish","cool fish"},new string[]{"Yes","No","new"});
        }
    }

}
