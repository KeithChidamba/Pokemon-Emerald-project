using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum InteractionOptions
{
    None,Battle,
    Interact,SellItem,HealPokemon,OpenPokemonStorage,OpenItemStorage,
    ReceiveGiftPokemon,ViewControls,Custom
}
public class DialogueOptionsEventHandler : MonoBehaviour,IInjectable
{
    public GameObject pokeballPrefab;
    
    private Interaction _currentInteraction;

    private readonly Dictionary<InteractionOptions, Action> _interactionMethods = new ();
    public event Action<Interaction,int> OnInteractionOptionChosen;
    public event Action<Interaction> OnEventInteraction;
    public event Action<OverworldInteractable,int> OnOverworldInteractionOptionChosen;
    
    private DialogueHandler _dialogueHandler;
    private PokemonPartyHandler _playerParty;
    private PlayerBagHandler _playerBagHandler;
    private GameUiHandler gameUiHandler;
    private PokemonStorageHandler _pokemonStorage;
    private BattleHandler _battleHandler;
    private PlayerMovementHandler _playerMovementHandler;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _playerParty = container.Resolve<PokemonPartyHandler>();
        gameUiHandler = container.Resolve<GameUiHandler>();
        _playerBagHandler = container.Resolve<PlayerBagHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonStorage = container.Resolve<PokemonStorageHandler>();
        _playerMovementHandler = container.Resolve<PlayerMovementHandler>();
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
    //overworld interactions
    public void ExitGame()
    {
        _dialogueHandler.DisplayCustomOptions("Are you sure you want to exit?, you will lose unsaved data!"
             , new[]{"Yes", "No"},new Action[] { CloseApplication, _dialogueHandler.EndDialogue });
        void CloseApplication()
        {
            _dialogueHandler.EndDialogue();
            Application.Quit();
        }
    }
    void ViewControls()
    {
        _dialogueHandler.EndDialogue(); 
        gameUiHandler.ViewKeyBinds();
    }
    void Battle()
    {
        _dialogueHandler.EndDialogue(); 
        StartCoroutine(_battleHandler.SetBattleTypeAndStart(
            _currentInteraction.GetModule<TrainerBattleInteractionInfo>().data));
    }
    
    void HealPokemon()
    {
        _playerMovementHandler.RestrictPlayerMovement(MovementRestrictor.OverworldAction);
        SoundManager.Play(JingleId.Healed);
        _playerParty.HealPartyPokemon();
        StartCoroutine(PlayPokeballAnimation());
        return;
        IEnumerator PlayPokeballAnimation()
        {
            var pokeballFlashImages = new List<Image>();
            var pokeballobjects = new List<GameObject>();
            const int columns = 2;

            RectTransform parent = pokeballPrefab.transform.parent.GetComponent<RectTransform>();
            RectTransform prefabRect = pokeballPrefab.GetComponent<RectTransform>();

            float width = prefabRect.rect.width;
            float height = prefabRect.rect.height;

            for (int i = 0; i < _playerParty.Party.Count; i++)
            {
                var newPokeball = Instantiate(pokeballPrefab, parent);
                newPokeball.SetActive(true);
                pokeballobjects.Add(newPokeball);
            
                RectTransform rect = newPokeball.GetComponent<RectTransform>();

                int row = i / columns;
                int column = i % columns;

                if (i > 0)
                {
                    rect.anchoredPosition = prefabRect.anchoredPosition 
                                            + new Vector2(column * width, -row * height);
                }
                pokeballFlashImages.Add(newPokeball.transform.GetChild(0).GetComponent<Image>());
                yield return new WaitForSeconds(.25f);
            }
            //Flashing
            const int flashCount = 4;
            const float totalDuration = 4f;
            const float fadeDuration = totalDuration / (flashCount * 2);

            for (int i = 0; i < flashCount; i++)
            {
                // White -> Yellow
                foreach (var image in pokeballFlashImages)
                {
                    StartCoroutine(Utility.FadeImage(
                        image,
                        new Color(1,1,1,0),
                        new Color(1,1,0,1),
                        fadeDuration
                    ));
                }
                yield return new WaitForSeconds(fadeDuration);

                // Yellow -> White
                foreach (var image in pokeballFlashImages)
                {
                    StartCoroutine(Utility.FadeImage(
                        image,
                        new Color(1,1,0,1),
                        new Color(1,1,1,0),
                        fadeDuration
                    ));
                }

                yield return new WaitForSeconds(fadeDuration);
            }
            _dialogueHandler.DisplayDetails("Your pokemon have been healed, you're welcome!");
            foreach (var obj in pokeballobjects)
            {
                Destroy(obj);
            }
            _playerMovementHandler.AllowPlayerMovement(MovementRestrictor.OverworldAction);
        }
    }
    void SellItem()
    {
        _playerBagHandler.currentBagUsage = BagUsage.SellingView;
        gameUiHandler.ValidateBagView();
    }
    void OpenPokemonStorage()
    {
        _dialogueHandler.EndDialogue(); 
        SoundManager.Play(UiId.PcOn);
        gameUiHandler.ViewPokemonStorage();
    }

    void OpenItemStorage()
    {
        _dialogueHandler.EndDialogue(); 
        gameUiHandler.ViewItemStorage();
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
    public void AlertOverworldInteraction(OverworldInteractable interactable,int optionIndex)
    {
        OnOverworldInteractionOptionChosen?.Invoke(interactable,optionIndex);
    }
    public void CompleteEventInteraction(Interaction interaction)
    {
        var interactionOption = interaction.interactionOptions[0];
        _currentInteraction = interaction;
        OnEventInteraction?.Invoke(interaction);
        if (_interactionMethods.TryGetValue(interactionOption,out var method)) method();
    }
    public void CompleteInteraction(Interaction interaction,int optionIndex)
    {
        OnInteractionOptionChosen?.Invoke(interaction,optionIndex);
        SoundManager.Play(UiId.Select);
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
        _dialogueHandler.AllowDialogueExit();
        
        _currentInteraction = interaction;
        if (_interactionMethods.TryGetValue(interactionOption,out var method))
            method();
        else
            Debug.Log("couldn't find method for interaction: " + interactionOption);
    }
}

