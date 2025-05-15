using System;
using System.Collections.Generic;
using UnityEngine;


public class Options_manager : MonoBehaviour
{
    private Interaction _currentInteraction;
    public bool playerInBattle;
    public bool selectedNewMoveOption;
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
        _interactionMethods.Add("Battle",Battle);
        _interactionMethods.Add("LearnMove",LearnMove);
        _interactionMethods.Add("SkipMove",SkipMove);
        _interactionMethods.Add("Fish",Fish);
        _interactionMethods.Add("Interact",Interact);
        _interactionMethods.Add("ViewMarketDelayed",ViewMarketDelayed);
        _interactionMethods.Add("SellItem",SellItem);
        _interactionMethods.Add("BuyMore",BuyMore);
        _interactionMethods.Add("DontBuyMore",DontBuyMore);
        _interactionMethods.Add("HealPokemon",HealPokemon);
        _interactionMethods.Add("OpenPokemonStorage",OpenPokemonStorage);
        _interactionMethods.Add("PickBerryFromTree",PickBerryFromTree);
        _interactionMethods.Add("ReceiveGiftPokemon",ReceiveGiftPokemon);
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
        var battleType = _currentInteraction.resultMessage;
        Battle_handler.Instance.SetBattleType(_currentInteraction.additionalInfo,battleType);
    }

    void LearnMove()
    {        
        Pokemon_Details.Instance.learningMove = true;
        Pokemon_Details.Instance.OnMoveSelected += PokemonOperations.LearnSelectedMove;
        Pokemon_Details.Instance.LoadDetails(PokemonOperations.CurrentPkm);
        Dialogue_handler.instance.Battle_Info("Which move will you replace?");
        selectedNewMoveOption = false;
    }
    void SkipMove()
    {
        Pokemon_Details.Instance.learningMove = false;
        PokemonOperations.LearningNewMove = false;
        Dialogue_handler.instance.Battle_Info(PokemonOperations.CurrentPkm.Pokemon_name+" did not learn "+PokemonOperations.NewMove.Move_name);
        selectedNewMoveOption = false;
        Battle_handler.Instance.levelUpQueue.RemoveAll(p=>p.pokemon==PokemonOperations.CurrentPkm);
    }
    void HealPokemon()
    {
        overworld_actions.Instance.doingAction = true;
        Dialogue_handler.instance.Write_Info(_currentInteraction.resultMessage, "Details");
        for (int i = 0; i < Pokemon_party.Instance.numMembers; i++)
        {
            var pokemon = Pokemon_party.Instance.party[i];
            pokemon.HP = pokemon.max_HP;
            foreach (var move in pokemon.move_set)
                move.Powerpoints = move.max_Powerpoints;
            pokemon.Status_effect = "None";
        }
        overworld_actions.Instance.doingAction = false;
        Dialogue_handler.instance.Write_Info("Your pokemon have been healed, you're welcome!", "Details");
    }
    void OpenPokemonStorage()
    {
        Dialogue_handler.instance.Write_Info(_currentInteraction.resultMessage, "Details");
        pokemon_storage.Instance.OpenPC();
        overworld_actions.Instance.usingUI = true;
    }
    void BuyMore()
    {
        Dialogue_handler.instance.Dialouge_off();
        Dialogue_handler.instance.Write_Info(_currentInteraction.resultMessage, "Details");
        ViewMarketDelayed();
    }
    void DontBuyMore()
    {
        Dialogue_handler.instance.Write_Info("Have a great day!", "Details",1f);
    }
    void ReceiveGiftPokemon()
    {
        var pokemonName = _currentInteraction.resultMessage;
        var pokemon = Resources.Load<Pokemon>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + pokemonName +"/"+ pokemonName);
        Pokemon_party.Instance.AddMember(pokemon);
        Dialogue_handler.instance.Dialouge_off();
        Dialogue_handler.instance.Write_Info("You got a " + pokemon.Pokemon_name, "Details");
        starterPokemonGiftEvent.PickGiftPokemon(pokemonName);
    }
    void Interact()
    {
        Dialogue_handler.instance.Write_Info(_currentInteraction.resultMessage, "Details",2f);
    }
    void Fish()
    {
        overworld_actions.Instance.doingAction = true;
        overworld_actions.Instance.manager.change_animation_state(overworld_actions.Instance.manager.Fishing_Start);
        Dialogue_handler.instance.Write_Info(_currentInteraction.resultMessage, "Details");
    }
    void SellItem()
    {
        Dialogue_handler.instance.Dialouge_off();
        Bag.Instance.sellingItems = true;
        Game_ui_manager.Instance.ViewBag();
    }
    void ViewMarketDelayed()//used in interaction as well
    {
        Invoke(nameof(ViewMarket),0.4f);
    }
    void ViewMarket()
    {
        Dialogue_handler.instance.Dialouge_off();
        Game_ui_manager.Instance.ViewMarket();
    }
    void PickBerryFromTree()
    {
        Dialogue_handler.instance.Dialouge_off();
        var berry = _currentInteraction.resultMessage;
        var bry = Resources.Load<Item>("Pokemon_project_assets/Player_obj/Bag/" + berry);
        Bag.Instance.AddItem(Obj_Instance.CreateItem(bry));
        Dialogue_handler.instance.Write_Info("You picked up a "+berry, "Details",1f);
    }
    public void Complete_Interaction(Interaction interaction,int option)
    {
        _currentInteraction = interaction;
        var methodName = interaction.interactionOptions[option].Replace(" ", "");
        if (_interactionMethods.TryGetValue(methodName,out var method))
            method();
        else
            Debug.Log("couldn't find method for interaction: " + methodName);
        if (interaction.interactionType == "List")
        {
            //list logic, might be useful later 
            //Dialogue_handler.instance.Write_Info("Would you like to fish for pokemon", "List", "fishing...",new string[]{"Fish","No fish","cool fish"},new string[]{"Yes","No","new"});
        }
    }

}
