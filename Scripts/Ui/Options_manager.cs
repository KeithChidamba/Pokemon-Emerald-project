using System;
using System.Collections.Generic;
using UnityEngine;


public class Options_manager : MonoBehaviour
{
    private Interaction _currentInteraction;
    public bool playerInBattle;
    [SerializeField] private Recieve_Pokemon starterPokemonGiftEvent;
    public static Options_manager Instance;
    private readonly Dictionary<string, Action> _interactionMethods = new ();
    public event Action<Overworld_interactable> OnInteractionTriggered;
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
        _interactionMethods.Add("CloseApplication",CloseApplication);
        _interactionMethods.Add("Battle",Battle);
        _interactionMethods.Add("LearnMove",LearnMove);
        _interactionMethods.Add("SkipMove",SkipMove);
        _interactionMethods.Add("Fish",Fish);
        _interactionMethods.Add("Interact",Interact);
        _interactionMethods.Add("ViewMarketDelayed",ViewMarketDelayed);
        _interactionMethods.Add("SellItem",SellItem);
        _interactionMethods.Add("BuyMore",BuyMore);
        _interactionMethods.Add("LeaveStore",LeaveStore);
        _interactionMethods.Add("HealPokemon",HealPokemon);
        _interactionMethods.Add("OpenPokemonStorage",OpenPokemonStorage);
        _interactionMethods.Add("PickBerryFromTree",PickBerryFromTree);
        _interactionMethods.Add("ReceiveGiftPokemon",ReceiveGiftPokemon);
    }

    void CloseApplication()
    {
        Dialogue_handler.Instance.EndDialogue();
        Application.Quit();
    }
    public void ExitGame()
    {
        Dialogue_handler.Instance.DisplayList("Are you sure you want to exit?, you will lose unsaved data!",
             "Good bye!", new[]{ "CloseApplication",""}, new[]{"Yes", "No"});
    }
    void Battle()
    {
        var battleType = _currentInteraction.resultMessage;
        var alivePokemon = Pokemon_party.Instance.GetLivingPokemon();
        if (alivePokemon.Count < 2 && battleType.ToLower().Contains("double"))
        {//if double battle enemy but you don't have enough pokemon alive
            Battle_handler.Instance.SetBattleType(_currentInteraction.additionalInfo,"single");
            return;
        }
        Battle_handler.Instance.SetBattleType(_currentInteraction.additionalInfo,battleType);
    }

    void LearnMove()
    {        
        Pokemon_Details.Instance.learningMove = true;
        Pokemon_Details.Instance.OnMoveSelected += PokemonOperations.LearnSelectedMove;
        Pokemon_Details.Instance.LoadDetails(PokemonOperations.CurrentPokemon);
        Dialogue_handler.Instance.DisplayBattleInfo("Which move will you replace?",false);
    }
    void SkipMove()
    {
        Game_ui_manager.Instance.canExitParty = true;
        Pokemon_Details.Instance.learningMove = false;
        PokemonOperations.SelectingMoveReplacement = false;
        PokemonOperations.LearningNewMove = false;
        Dialogue_handler.Instance.DisplayBattleInfo(PokemonOperations.CurrentPokemon.pokemonName +
                                                    " did not learn "+PokemonOperations.NewMove.moveName,true);
    }

    void HealPokemon()
    {
        overworld_actions.Instance.doingAction = true;
        Dialogue_handler.Instance.DisplayInfo(_currentInteraction.resultMessage, "Details");
        HealPartyPokemon();
        overworld_actions.Instance.doingAction = false;
        Dialogue_handler.Instance.DisplayInfo("Your pokemon have been healed, you're welcome!", "Details");
    }
    public void HealPartyPokemon()
    {
        for (int i = 0; i < Pokemon_party.Instance.numMembers; i++)
        {
            var pokemon = Pokemon_party.Instance.party[i];
            pokemon.hp = pokemon.maxHp;
            foreach (var move in pokemon.moveSet)
                move.powerpoints = move.maxPowerpoints;
            pokemon.statusEffect = "None";
        }
    }
    void OpenPokemonStorage()
    {
        Dialogue_handler.Instance.DisplayInfo(_currentInteraction.resultMessage, "Details");
        pokemon_storage.Instance.OpenPC();
        overworld_actions.Instance.usingUI = true;
    }
    void BuyMore()
    {
        Dialogue_handler.Instance.EndDialogue();
        Dialogue_handler.Instance.DisplayInfo(_currentInteraction.resultMessage, "Details");
        ViewMarketDelayed();
    }
    void LeaveStore()
    {
        Dialogue_handler.Instance.DisplayInfo("Have a great day!", "Details",1f);
    }
    void ReceiveGiftPokemon()
    {
        if(pokemon_storage.Instance.MaxPokemonCapacity())
        {
            Dialogue_handler.Instance.DisplayInfo("Can no longer obtain more pokemon, free up space in pc!", "Details");
            return;
        }
        var pokemonName = _currentInteraction.resultMessage;
        var pokemon = Resources.Load<Pokemon>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + pokemonName +"/"+ pokemonName);
        pokemon.ChangeFriendshipLevel(120);
        pokemon.pokeballName = "Regular Pokeball";
        Pokemon_party.Instance.AddMember(pokemon);
        Dialogue_handler.Instance.EndDialogue();
        Dialogue_handler.Instance.DisplayInfo("You got a " + pokemon.pokemonName, "Details");
        starterPokemonGiftEvent.PickGiftPokemon(pokemonName);
    }
    void Interact()
    {
        Dialogue_handler.Instance.DisplayInfo(_currentInteraction.resultMessage, "Details",2f);
    }
    void Fish()
    {
        overworld_actions.Instance.doingAction = true;
        overworld_actions.Instance.manager.ChangeAnimationState(overworld_actions.Instance.manager.fishingStart);
        Dialogue_handler.Instance.DisplayInfo(_currentInteraction.resultMessage, "Details");
    }
    void SellItem()
    {
        Dialogue_handler.Instance.EndDialogue();
        Bag.Instance.sellingItems = true;
        Game_ui_manager.Instance.ViewBag();
    }
    void ViewMarketDelayed()//used in interaction as well
    {
        Invoke(nameof(ViewMarket),0.4f);
    }
    void ViewMarket()
    {
        Dialogue_handler.Instance.EndDialogue();
        Game_ui_manager.Instance.ViewMarket();
    }
    void PickBerryFromTree()
    {
        Dialogue_handler.Instance.EndDialogue();
        var berry = _currentInteraction.resultMessage;
        var berryAsset = Resources.Load<Item>("Pokemon_project_assets/Items/" + berry);
        Bag.Instance.AddItem(Obj_Instance.CreateItem(berryAsset));
        Dialogue_handler.Instance.DisplayInfo("You picked up a "+berry, "Details",1f);
    }
    public void CompleteInteraction(Interaction interaction,int option)
    {
        var methodName = interaction.interactionOptions[option].Replace(" ", "");
        if (methodName == string.Empty) { Dialogue_handler.Instance.EndDialogue(); return; }
        _currentInteraction = interaction;
        if (_interactionMethods.TryGetValue(methodName,out var method))
            method();
        else
            Debug.Log("couldn't find method for interaction: " + methodName);
    }
    public void CompleteInteraction(Overworld_interactable interactable,int option)
    {
        OnInteractionTriggered?.Invoke(interactable);
        CompleteInteraction(interactable.interaction,option);
    }
}
