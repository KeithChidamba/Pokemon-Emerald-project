using System;
using System.Collections.Generic;
using UnityEngine;

public enum InteractionOptions
{
    None,CloseApplication,Battle,LearnMove,SkipMove,
    Fish,Interact,SellItem,HealPokemon,OpenPokemonStorage,OpenItemStorage,
    ReceiveGiftPokemon,LeaveStore,ViewControls,
}
public class DialogueOptionsEventHandler : MonoBehaviour,IInjectable
{
    private Interaction _currentInteraction;

    private readonly Dictionary<InteractionOptions, Action> _interactionMethods = new ();
    public event Action<Interaction,int> OnInteractionOptionChosen;
    public event Action<Overworld_interactable,int> OnOverworldInteractionOptionChosen;
    
    private Dialogue_handler _dialogueHandler;
    private Pokemon_party _playerParty;
    private PokemonOperations _pokemonOperations;
    private Pokemon_Details _pokemonDetailsHandler;
    private Game_ui_manager _gameUIManager;
    private overworld_actions _overworldActionsHandler;
    private Bag _playerBag;
    private pokemon_storage _pokemonStorage;
    private Battle_handler _battleHandler;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _playerParty = container.Resolve<Pokemon_party>();
        _gameUIManager = container.Resolve<Game_ui_manager>();
        _pokemonOperations = container.Resolve<PokemonOperations>();
        _pokemonDetailsHandler = container.Resolve<Pokemon_Details>();
        _overworldActionsHandler = container.Resolve<overworld_actions>();
        _battleHandler = container.Resolve<Battle_handler>();
        _pokemonStorage = container.Resolve<pokemon_storage>();
        _playerBag = container.Resolve<Bag>();
        
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
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
        _dialogueHandler.EndDialogue();
        Application.Quit();
    }
    public void ExitGame()
    {
        _dialogueHandler.DisplayList("Are you sure you want to exit?, you will lose unsaved data!",
             new[]{ InteractionOptions.CloseApplication,InteractionOptions.None}
             , new[]{"Yes", "No"},"Good bye!");
    }

    void ViewControls()
    {
        _dialogueHandler.EndDialogue(); 
        _gameUIManager.ViewKeyBinds();
    }
    void Battle()
    {
        _dialogueHandler.EndDialogue(); 
        _battleHandler.SetBattleType(_currentInteraction.additionalInfo);
    }

    void LearnMove()
    {        
        _pokemonDetailsHandler.learningMove = true;
        _pokemonDetailsHandler.OnMoveSelected += _pokemonOperations.LearnSelectedMove;
        _dialogueHandler.DisplayBattleInfo("Which move will you replace?",false);
        _gameUIManager.ViewPartyPokemonDetails(_pokemonOperations.currentPokemon);
    }
    public void SkipMove()
    {
        _dialogueHandler.DeletePreviousOptions();
        _pokemonDetailsHandler.OnMoveSelected = null;
        _pokemonOperations.SelectingMoveReplacement = false;
        _pokemonOperations.LearningNewMove = false;
        _pokemonDetailsHandler.learningMove = false;
        _dialogueHandler.DisplayBattleInfo(_pokemonOperations.currentPokemon.pokemonName +
                                                    " did not learn "+_pokemonOperations.NewMoveAsset.moveName,false);
    }

    void HealPokemon()
    {
        HealPartyPokemon();
        _dialogueHandler.DisplayDetails("Your pokemon have been healed, you're welcome!");
    }
    public void HealPartyPokemon()
    {
        for (int i = 0; i < _playerParty.numMembers; i++)
        {
            var pokemon = _playerParty.party[i];
            pokemon.hp = pokemon.maxHp;
            foreach (var move in pokemon.moveSet)
                move.powerpoints = move.maxPowerpoints;
            pokemon.statusEffect = StatusEffect.None;
        }
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
        var pokemonName = _currentInteraction.additionalInfo[int.Parse(_currentInteraction.resultMessage)-1];
        var pokemon = Resources.Load<Pokemon>(SaveDataHandler.GetDirectory(AssetDirectory.Pokemon)
                                              + pokemonName +"/"+ pokemonName);
        
        _playerParty.AddMember(pokemon,isGiftPokemon:true);
        _dialogueHandler.EndDialogue();
        _dialogueHandler.DisplayDetails("You got a " + pokemon.pokemonName);
    }
    void Interact()
    {
        _dialogueHandler.DisplayDetails(_currentInteraction.resultMessage);
    }
    void Fish()
    {
        _overworldActionsHandler.manager.ChangeAnimationState(PlayerAnimationState.FishingStart);
        _dialogueHandler.DisplayDetails(_currentInteraction.resultMessage);
    }
    void SellItem()
    {
        _playerBag.currentBagUsage = BagUsage.SellingView;
        _gameUIManager.ViewBag();
    }

    void LeaveStore()
    {
        _dialogueHandler.DisplayDetails("Have a great day!");
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

