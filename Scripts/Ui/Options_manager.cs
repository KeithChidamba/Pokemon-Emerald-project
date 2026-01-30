using System;
using System.Collections.Generic;
using UnityEngine;


public enum InteractionOptions
{
    None,CloseApplication,Battle,LearnMove,SkipMove,
    Fish,Interact,SellItem,HealPokemon,OpenPokemonStorage,OpenItemStorage,ReceiveGiftPokemon,LeaveStore,ViewControls
}
public class Options_manager : MonoBehaviour
{
    private Interaction _currentInteraction;
    private Overworld_interactable _currentInteractable;
    public bool playerInBattle;
    [SerializeField] private Recieve_Pokemon starterPokemonGiftEvent;
    public static Options_manager Instance;
    private readonly Dictionary<InteractionOptions, Action> _interactionMethods = new ();
    public event Action<Overworld_interactable,int> OnInteractionOptionChosen;

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
        _interactionMethods.Add(InteractionOptions.CloseApplication,CloseApplication);
        _interactionMethods.Add(InteractionOptions.Battle,Battle);
        _interactionMethods.Add(InteractionOptions.LearnMove,LearnMove);
        _interactionMethods.Add(InteractionOptions.SkipMove,SkipMove);
        _interactionMethods.Add(InteractionOptions.Fish,Fish);
        _interactionMethods.Add(InteractionOptions.Interact,Interact);
        _interactionMethods.Add(InteractionOptions.SellItem,SellItem);
        _interactionMethods.Add(InteractionOptions.LeaveStore,LeaveStore);
        _interactionMethods.Add(InteractionOptions.HealPokemon,HealPokemon);
        _interactionMethods.Add(InteractionOptions.OpenPokemonStorage,OpenPokemonStorage);
        _interactionMethods.Add(InteractionOptions.OpenItemStorage,OpenItemStorage);
        _interactionMethods.Add(InteractionOptions.ReceiveGiftPokemon,ReceiveGiftPokemon);
        _interactionMethods.Add(InteractionOptions.ViewControls,ViewControls);
    }

    void CloseApplication()
    {
        Dialogue_handler.Instance.EndDialogue();
        Application.Quit();
    }
    public void ExitGame()
    {
        Dialogue_handler.Instance.DisplayList("Are you sure you want to exit?, you will lose unsaved data!",
             "Good bye!", 
             new[]{ InteractionOptions.CloseApplication,InteractionOptions.None}
             , new[]{"Yes", "No"});
    }

    void ViewControls()
    {
        Dialogue_handler.Instance.EndDialogue(); 
        Game_ui_manager.Instance.ViewKeyBinds();
    }
    void Battle()
    {
        Dialogue_handler.Instance.EndDialogue(); 
        Battle_handler.Instance.SetBattleType(_currentInteraction.additionalInfo);
    }

    void LearnMove()
    {        
        Pokemon_Details.Instance.learningMove = true;
        Pokemon_Details.Instance.OnMoveSelected += PokemonOperations.LearnSelectedMove;
        Dialogue_handler.Instance.DisplayBattleInfo("Which move will you replace?",false);
        Game_ui_manager.Instance.ViewPartyPokemonDetails(PokemonOperations.CurrentPokemon);
    }
    public void SkipMove()
    {
        Dialogue_handler.Instance.DeletePreviousOptions();
        Pokemon_Details.Instance.OnMoveSelected = null;
        PokemonOperations.SelectingMoveReplacement = false;
        PokemonOperations.LearningNewMove = false;
        Pokemon_Details.Instance.learningMove = false;
        Dialogue_handler.Instance.DisplayBattleInfo(PokemonOperations.CurrentPokemon.pokemonName +
                                                    " did not learn "+PokemonOperations.NewMoveAsset.moveName,false);
    }

    void HealPokemon()
    {
        overworld_actions.Instance.doingAction = true;
        Dialogue_handler.Instance.DisplayDetails(_currentInteraction.resultMessage);
        HealPartyPokemon();
        overworld_actions.Instance.doingAction = false;
        Dialogue_handler.Instance.DisplayDetails("Your pokemon have been healed, you're welcome!");
    }
    public void HealPartyPokemon()
    {
        for (int i = 0; i < Pokemon_party.Instance.numMembers; i++)
        {
            var pokemon = Pokemon_party.Instance.party[i];
            pokemon.hp = pokemon.maxHp;
            foreach (var move in pokemon.moveSet)
                move.powerpoints = move.maxPowerpoints;
            pokemon.statusEffect = StatusEffect.None;
        }
    }
    void OpenPokemonStorage()
    {
        Dialogue_handler.Instance.EndDialogue(); 
        Game_ui_manager.Instance.ViewPokemonStorage();
    }

    void OpenItemStorage()
    {
        Dialogue_handler.Instance.EndDialogue(); 
        Game_ui_manager.Instance.ViewItemStorage();
    }
    void ReceiveGiftPokemon()
    {
        if(pokemon_storage.Instance.MaxPokemonCapacity())
        {
            Dialogue_handler.Instance.DisplayDetails("Can no longer obtain more pokemon, free up space in pc!");
            return;
        }
        var pokemonName = _currentInteraction.resultMessage;
        var pokemon = Resources.Load<Pokemon>(Save_manager.GetDirectory(AssetDirectory.Pokemon)
                                              + pokemonName +"/"+ pokemonName);
        Pokemon_party.Instance.AddMember(pokemon,isGiftPokemon:true);
        Dialogue_handler.Instance.EndDialogue();
        Dialogue_handler.Instance.DisplayDetails("You got a " + pokemon.pokemonName);
        starterPokemonGiftEvent.PickGiftPokemon(pokemonName);
    }
    void Interact()
    {
        Dialogue_handler.Instance.DisplayDetails(_currentInteraction.resultMessage);
    }
    void Fish()
    {
        overworld_actions.Instance.doingAction = true;
        overworld_actions.Instance.manager.ChangeAnimationState(overworld_actions.Instance.manager.fishingStart);
        Dialogue_handler.Instance.DisplayDetails(_currentInteraction.resultMessage);
    }
    void SellItem()
    {
        Bag.Instance.currentBagUsage = BagUsage.SellingView;
        Game_ui_manager.Instance.ViewBag();
    }

    void LeaveStore()
    {
        Dialogue_handler.Instance.DisplayDetails("Have a great day!");
    }
    public void CompleteEventInteraction(Overworld_interactable interactable)
    {
        var interactionOption = interactable.interaction.interactionOptions[0];
        _currentInteraction = interactable.interaction;
        OnInteractionOptionChosen?.Invoke(interactable,0);
        if (_interactionMethods.TryGetValue(interactionOption,out var method)) method();
    }
    public void CompleteInteraction(Interaction interaction,int optionIndex)
    {
        var interactionOption = interaction.interactionOptions[optionIndex];
        if (interactionOption == InteractionOptions.None)
        {
            Dialogue_handler.Instance.EndDialogue(); 
            return;
        }
        
        Dialogue_handler.Instance.DeletePreviousOptions();
        Dialogue_handler.Instance.canExitDialogue = true;
        
        _currentInteraction = interaction;
        if (_interactionMethods.TryGetValue(interactionOption,out var method))
            method();
        else
            Debug.Log("couldn't find method for interaction: " + interactionOption);
    }
    public void CompleteInteraction(Overworld_interactable interactable,int optionIndex)
    {
        _currentInteractable = interactable;
        OnInteractionOptionChosen?.Invoke(interactable,optionIndex);
        if (_currentInteractable.interaction.hasSeparateLogicHandler
            || _currentInteractable.interaction.dialogueType != DialogType.Options)
        {
            return;
        }

        CompleteInteraction(_currentInteractable.interaction,optionIndex);
    }
}

