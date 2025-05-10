using System;
using System.Collections.Generic;
using UnityEngine;


public class Options_manager : MonoBehaviour
{
    private Interaction _currentInteraction;
    public bool playerInBattle = false;
    public bool selectedNewMoveOption = false;
    [SerializeField] private Recieve_Pokemon starterPokemonGiftEvent;
    public static Options_manager Instance;
    private readonly Dictionary<string, Action> _interactionMethods = new ();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _interactionMethods.Add("ExitGame",ExitGame);
    }

    void ExitGame()
    {
        Dialogue_handler.instance.Dialouge_off();
        Game_Load.Instance.ExitGame();
    }
    public void ExitToMenu()
    {
        Dialogue_handler.instance.Write_Info("Are you sure you want to exit?, you will lose unsaved data!", "Options", "ExitGame","Good bye!","","Yes","No");
    }
    void Battle()
    {
        string battleType = _currentInteraction.ResultMessage;
        Battle_handler.Instance.SetBattleType(_currentInteraction.AdditionalInfo,battleType);
    }

    void Learn_Move()
    {        
        Pokemon_Details.instance.LearningMove = true;
        Pokemon_Details.instance.OnMoveSelected += PokemonOperations.LearnSelectedMove;
        Pokemon_Details.instance.Load_Details(PokemonOperations.CurrentPkm);
        Dialogue_handler.instance.Battle_Info("Which move will you replace?");
        selectedNewMoveOption = false;
    }
    void Skip_Move()
    {
        Pokemon_Details.instance.LearningMove = false;
        PokemonOperations.LearningNewMove = false;
        Dialogue_handler.instance.Battle_Info(PokemonOperations.CurrentPkm.Pokemon_name+" did not learn "+PokemonOperations.NewMove.Move_name);
        selectedNewMoveOption = false;
        Battle_handler.Instance.levelUpQueue.RemoveAll(p=>p.pokemon==PokemonOperations.CurrentPkm);
    }
    void Heal_Pokemon()
    {
        overworld_actions.instance.doing_action = true;
        Dialogue_handler.instance.Write_Info(_currentInteraction.ResultMessage, "Details");
        for (int i = 0; i < Pokemon_party.instance.num_members; i++)
        {
            Pokemon_party.instance.party[i].HP = Pokemon_party.instance.party[i].max_HP;
            foreach (Move m in Pokemon_party.instance.party[i].move_set)
                m.Powerpoints = m.max_Powerpoints;
            Pokemon_party.instance.party[i].Status_effect = "None";
        }
        overworld_actions.instance.doing_action = false;
        Dialogue_handler.instance.Write_Info("Your pokemon have been healed, you're welcome!", "Details");
    }
    void PC_storage()
    {
        Dialogue_handler.instance.Write_Info(_currentInteraction.ResultMessage, "Details");
        pokemon_storage.instance.Open_pc();
        overworld_actions.instance.using_ui = true;
    }
    void Buy_More()
    {
        Dialogue_handler.instance.Dialouge_off();
        Dialogue_handler.instance.Write_Info(_currentInteraction.ResultMessage, "Details");
        ViewMarketDelayed();
    }
    void Dont_Buy()
    {
        Dialogue_handler.instance.Write_Info("Have a great day!", "Details",1f);
    }
    void Gift_pkm()
    {
        var pokemonName = _currentInteraction.ResultMessage;
        var pokemon = Resources.Load<Pokemon>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + pokemonName +"/"+ pokemonName);
        Pokemon_party.instance.Add_Member(pokemon);
        Dialogue_handler.instance.Dialouge_off();
        Dialogue_handler.instance.Write_Info("You got a " + pokemon.Pokemon_name, "Details");
        starterPokemonGiftEvent.PickGiftPokemon(pokemonName);
    }
    void Interact()
    {
        Dialogue_handler.instance.Write_Info(_currentInteraction.ResultMessage, "Details",2f);
    }
    void Fish()
    {
        overworld_actions.instance.doing_action = true;
        overworld_actions.instance.manager.change_animation_state(overworld_actions.instance.manager.Fishing_Start);
        Dialogue_handler.instance.Write_Info(_currentInteraction.ResultMessage, "Details");
    }
    void Sell_item()
    {
        Dialogue_handler.instance.Dialouge_off();
        Bag.instance.Selling_items = true;
        Game_ui_manager.instance.View_Bag();
    }
    void ViewMarketDelayed()//used in interaction as well
    {
        Invoke(nameof(View_Market),0.7f);
    }
    void View_Market()
    {
        Dialogue_handler.instance.Dialouge_off();
        Game_ui_manager.instance.view_market();
    }
    void Pick_Berry()
    {
        Dialogue_handler.instance.Dialouge_off();
        string berry = _currentInteraction.ResultMessage;
        Item bry = Resources.Load<Item>("Pokemon_project_assets/Player_obj/Bag/" + berry);
        Bag.instance.Add_item(Obj_Instance.set_Item(bry));
        Dialogue_handler.instance.Write_Info("You picked up a "+berry, "Details",1f);
    }
    public void Complete_Interaction(Interaction interaction,int option)
    {
        _currentInteraction = interaction;
        if (_interactionMethods.TryGetValue(interaction.InteractionOptions[option],out Action method))
            method();
        else
            Debug.Log("couldn't find method for interaction: " + interaction.InteractionOptions[option]);
        if (interaction.InteractionType == "List")
        {
            //list logic, might be useful later 
            //Dialogue_handler.instance.Write_Info("Would you like to fish for pokemon", "List", "fishing...",new string[]{"Fish","No fish","cool fish"},new string[]{"Yes","No","new"});
        }
    }

}
