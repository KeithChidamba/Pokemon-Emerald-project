using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class overworld_actions : MonoBehaviour,IInjectable
{
    public Animation_manager manager;
    public bool fishing;
    [SerializeField] private bool pokemonBitingPole;
    public FishingEncounterTable fishingTable;
    
    public Item equippedSpecialItem;
    private bool _canUseEquippedItem;
    private Equipable _currentEquippedItem;
    public event Action<Equipable> OnItemEquipped;
    public event Action<Equipable> OnItemUnequipped;
    public event Action OnActionComplete;
    
    private Dialogue_handler _dialogueHandler;
    private Game_ui_manager _gameUIManager;
    private Player_movement _playerMovementHandler;
    private Encounter_handler _encounterHandler;
    private Game_Load _gameLoadingHandler;
    private Battle_handler _battleHandler;
    private InputStateHandler _inputStateHandler;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _encounterHandler = container.Resolve<Encounter_handler>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        _gameUIManager = container.Resolve<Game_ui_manager>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        _battleHandler = container.Resolve<Battle_handler>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
    }
    public void OnInject()
    {
        _gameLoadingHandler.OnGameStarted += () => _canUseEquippedItem = true;
        manager.OnFishingStart += StartFishingAction;
        _canUseEquippedItem = false;
        gameObject.SetActive(true);
    }

    public void EquipItem(Item item)
    {
        if (item == null) return;//there was no item equipped in save data
        equippedSpecialItem = item;
        _currentEquippedItem = equippedSpecialItem.GetDynamicModule<EquipableItemInfo>().equipableItem;
        OnItemEquipped?.Invoke(_currentEquippedItem);
        if(_gameUIManager.usingUI)
        {
            _dialogueHandler.DisplayDetails("Equipped " + equippedSpecialItem.itemName);
        }
     
        _gameLoadingHandler.playerData.equippedItemName = equippedSpecialItem.itemName;
    }
    public bool IsEquipped(Equipable equipable = Equipable.None, Item item = null)
    {
        if (!_canUseEquippedItem || !ItemEquipped())
        {
            return false;
        }
        if (item == null)
        {
            return _currentEquippedItem == equipable;
        }
        //distinguish item of same type
        return equippedSpecialItem.itemName == item.itemName;
    }
    public void UnequipItem(Item item)
    {
        var previousItem = _currentEquippedItem;
        _currentEquippedItem = Equipable.None;
        equippedSpecialItem = null;
        OnItemUnequipped?.Invoke(previousItem);
        if(_gameUIManager.usingUI)
        {
            _dialogueHandler.DisplayDetails("Unequipped " + item.itemName);
        }
        _gameLoadingHandler.playerData.equippedItemName = string.Empty;
    }
    public bool ItemEquipped()
    {
        return equippedSpecialItem != null;
    }
    void Update()
    {
        if (InputSourceHandler.InputPressed(ControlEvent.UseSpecialItem))
        {
            if (!_inputStateHandler.IsEmptyState) return;
            if (!ItemEquipped())
            {
                if (!_canUseEquippedItem) return;
                _dialogueHandler.DisplayDetails("No item has been equipped");
            }
        }  
        if (pokemonBitingPole && InputSourceHandler.InputPressed(ControlEvent.Confirm))
        {
            pokemonBitingPole = false;
            _dialogueHandler.EndDialogue();
            _encounterHandler.TriggerFishingEncounter(fishingTable,equippedSpecialItem);
        }
        if (fishing && !_battleHandler.battleInProgress)
        {
            //cancel fishing
            if (InputSourceHandler.InputPressed(ControlEvent.UseSpecialItem))
            {
                StartCoroutine(EndFishingAction());
            }
        }
    }

    private IEnumerator TryFishing()
    {
        fishing = true;
        manager.ChangeAnimationState(PlayerAnimationState.FishingIdle);
        var random = Utility.RandomRange(1, 11);
        yield return new WaitForSeconds(1f);//allow fishing cancel
        if (!fishing) yield break; //if fishing canceled early
        if (random < 5)
        {
            pokemonBitingPole = true;
            _dialogueHandler.DisplayDetails("Oh!, a Bite!, Press Z");
            yield return new WaitForSeconds((2 * (random / 10f)) + 1f);
            if (pokemonBitingPole)
            {
                _dialogueHandler.DisplayDetails("It got away");
                yield return EndFishingAction();
            }
        }
        else
        {
            _dialogueHandler.DisplayDetails("Dang...nothing");
            yield return EndFishingAction();
        }
    }

    public void PlayFishingAnimation()
    {
        manager.ChangeAnimationState(PlayerAnimationState.FishingStart);
        _dialogueHandler.DisplayDetails("fishing...");
    }
    private void StartFishingAction()
    {
        _playerMovementHandler.RestrictPlayerMovement(MovementRestrictor.OverworldAction);
        StartCoroutine(TryFishing());
    }

    private IEnumerator EndFishingAction()
    {
        fishing = false;
        pokemonBitingPole = false;
        manager.ChangeAnimationState(PlayerAnimationState.FishingEnd);
        yield return new WaitForSeconds(1f);
        OnActionComplete?.Invoke();
    }
    public void EndFishing()
    {
        StartCoroutine(EndFishingAction());
    }
    public IEnumerator WaterTrees(BerryTree treeToWater)
    {
        manager.ChangeAnimationState(PlayerAnimationState.Watering);
        _dialogueHandler.DisplayDetails("The tree is being watered",false);
        yield return new WaitForSeconds(2f);
        _dialogueHandler.EndDialogue();
        treeToWater.CompleteWateringEvent();
        manager.ChangeAnimationState(PlayerAnimationState.PlayerWalk);
    }
}
