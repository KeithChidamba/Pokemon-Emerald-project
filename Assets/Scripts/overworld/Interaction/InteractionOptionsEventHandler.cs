using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum InteractionOptions
{
    None,Battle, Custom,SellItem,HealPokemon,OpenPokemonStorage,
    OpenItemStorage, HandleInDynamicClass
}
public class InteractionOptionsEventHandler : MonoBehaviour,IInjectable
{
    [SerializeField]private GameObject pokeballPrefab;
    
    private Interaction _currentInteraction;

    private readonly Dictionary<InteractionOptions, Action> _interactionMethods = new ();
    public event Action<Interaction,int> OnInteractionOptionChosen;
    public event Action<Interaction> OnEventInteraction;
    public event Action<OverworldInteractable,int> OnOverworldInteractionOptionChosen;
    
    private DialogueHandler _dialogueHandler;
    private PokemonPartyHandler _playerParty;
    private PlayerBagHandler _playerBagHandler;
    private GameUiHandler gameUiHandler;
    private BattleHandler _battleHandler;
    private PlayerMovementHandler _playerMovementHandler;
    
    private ServiceContainer _container;
    
    public void Inject(ServiceContainer container)
    {
        _container = container;
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _playerParty = container.Resolve<PokemonPartyHandler>();
        gameUiHandler = container.Resolve<GameUiHandler>();
        _playerBagHandler = container.Resolve<PlayerBagHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        _playerMovementHandler = container.Resolve<PlayerMovementHandler>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _interactionMethods.Add(InteractionOptions.Battle,Battle);
        _interactionMethods.Add(InteractionOptions.HealPokemon,HealPokemon);
        _interactionMethods.Add(InteractionOptions.OpenPokemonStorage,OpenPokemonStorage);
        _interactionMethods.Add(InteractionOptions.SellItem,SellItem);
        _interactionMethods.Add(InteractionOptions.OpenItemStorage,OpenItemStorage);
        _interactionMethods.Add(InteractionOptions.HandleInDynamicClass,HandleInDynamicClass);
    }
    private void Battle()
    {
        _dialogueHandler.EndDialogue(); 
        StartCoroutine(_battleHandler.SetBattleTypeAndStart(
            _currentInteraction.GetModule<TrainerData>()));
    }
    private void HealPokemon()
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
    private void SellItem()
    {
        _playerBagHandler.currentBagUsage = BagUsage.SellingView;
        gameUiHandler.ValidateBagView();
    }
    private void OpenPokemonStorage()
    {
        _dialogueHandler.EndDialogue(); 
        SoundManager.Play(UiId.PcOn);
        gameUiHandler.ViewPokemonStorage();
    }
    private void OpenItemStorage()
    {
        _dialogueHandler.EndDialogue(); 
        gameUiHandler.ViewItemStorage();
    }
    private void HandleInDynamicClass()
    {
        _currentInteraction.dynamicInfoModules.First().HandleInternalLogic(_container);
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

