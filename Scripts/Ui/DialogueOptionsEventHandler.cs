using System;
using System.Collections.Generic;
using UnityEngine;

public enum InteractionOptions
{
    None,Battle,
    Interact,SellItem,HealPokemon,OpenPokemonStorage,OpenItemStorage,
    ReceiveGiftPokemon,ViewControls,Custom
}
public class DialogueOptionsEventHandler : MonoBehaviour,IInjectable
{
    private Interaction _currentInteraction;

    private readonly Dictionary<InteractionOptions, Action> _interactionMethods = new ();
    public event Action<Interaction,int> OnInteractionOptionChosen;
    public event Action<Overworld_interactable,int> OnOverworldInteractionOptionChosen;
    
    private Dialogue_handler _dialogueHandler;
    private Pokemon_party _playerParty;
    private Bag _playerBagHandler;
    private Game_ui_manager _gameUIManager;
    private pokemon_storage _pokemonStorage;
    private Battle_handler _battleHandler;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _playerParty = container.Resolve<Pokemon_party>();
        _gameUIManager = container.Resolve<Game_ui_manager>();
        _playerBagHandler = container.Resolve<Bag>();
        _battleHandler = container.Resolve<Battle_handler>();
        _pokemonStorage = container.Resolve<pokemon_storage>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _interactionMethods.Add(InteractionOptions.Battle,Battle);
        _interactionMethods.Add(InteractionOptions.Interact,Interact);
        _interactionMethods.Add(InteractionOptions.HealPokemon,HealPokemon);
        _interactionMethods.Add(InteractionOptions.OpenPokemonStorage,OpenPokemonStorage);
        _interactionMethods.Add(InteractionOptions.SellItem,SellItem);
        _interactionMethods.Add(InteractionOptions.OpenItemStorage,OpenItemStorage);
        _interactionMethods.Add(InteractionOptions.ReceiveGiftPokemon,ReceiveGiftPokemon);
        _interactionMethods.Add(InteractionOptions.ViewControls,ViewControls);
    }

    void CloseApplication()
    {
        _dialogueHandler.EndDialogue();
        Application.Quit();
    }
    public void ExitGame()
    {
        _dialogueHandler.DisplayCustomOptions("Are you sure you want to exit?, you will lose unsaved data!"
             , new[]{"Yes", "No"},new Action[] { CloseApplication, null },"Good bye!");
    }

    void ViewControls()
    {
        _dialogueHandler.EndDialogue(); 
        _gameUIManager.ViewKeyBinds();
    }
    void Battle()
    {
        _dialogueHandler.EndDialogue(); 
        _battleHandler.SetBattleType(_currentInteraction.GetModule<TrainerBattleInteractionInfo>());
    }
    
    void HealPokemon()
    {
        _playerParty.HealPartyPokemon();
        _dialogueHandler.DisplayDetails("Your pokemon have been healed, you're welcome!");
    }
    void SellItem()
    {
        _playerBagHandler.currentBagUsage = BagUsage.SellingView;
        _gameUIManager.ValidateBagView();
    }
    void OpenPokemonStorage()
    {
        _dialogueHandler.EndDialogue(); 
        _gameUIManager.ViewPokemonStorage();
    }

    void OpenItemStorage()
    {
        _dialogueHandler.EndDialogue(); 
        _gameUIManager.ViewItemStorage();
    }
    void ReceiveGiftPokemon()
    {
        if(_pokemonStorage.MaxPokemonCapacity())
        {
            _dialogueHandler.DisplayDetails("Can no longer obtain more pokemon, free up space in pc!");
            return;
        }
        var giftInteraction = _currentInteraction.GetModule<PokemonGiftInteractoinInfo>();
        _playerParty.AddGiftMember(giftInteraction);
    }
    void Interact()
    {
        _dialogueHandler.DisplayDetails(_currentInteraction.resultMessage);
    }
    public void AlertOverworldInteraction(Overworld_interactable interactable,int optionIndex)
    {
        OnOverworldInteractionOptionChosen?.Invoke(interactable,optionIndex);
    }
    public void CompleteEventInteraction(Interaction interaction)
    {
        var interactionOption = interaction.interactionOptions[0];
        _currentInteraction = interaction;
        OnInteractionOptionChosen?.Invoke(interaction,0);
        if (_interactionMethods.TryGetValue(interactionOption,out var method)) method();
    }
    public void CompleteInteraction(Interaction interaction,int optionIndex)
    {
        OnInteractionOptionChosen?.Invoke(interaction,optionIndex);
        var interactionOption = interaction.interactionOptions[optionIndex];
        if (interactionOption == InteractionOptions.Custom)
        {
            return;
        }
        if (interactionOption == InteractionOptions.None)
        {
            _dialogueHandler.EndDialogue(); 
            return;
        }
        
        _dialogueHandler.DeletePreviousOptions();
        _dialogueHandler.canExitDialogue = true;
        
        _currentInteraction = interaction;
        if (_interactionMethods.TryGetValue(interactionOption,out var method))
            method();
        else
            Debug.Log("couldn't find method for interaction: " + interactionOption);
    }
}

