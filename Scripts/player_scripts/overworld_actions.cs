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

    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _encounterHandler = container.Resolve<Encounter_handler>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        _gameUIManager = container.Resolve<Game_ui_manager>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        OnInject();
    }
    private void OnInject()
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
        _currentEquippedItem = equippedSpecialItem.GetModule<EquipableInfoModule>().equipableItem;
        OnItemEquipped?.Invoke(_currentEquippedItem);
        if(_gameUIManager.usingUI)
            _dialogueHandler.DisplayDetails("Equipped " + equippedSpecialItem.itemName);
     
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
        if (InputSourceHandler.InputPressed(ControlEvent.UseSpecialItem) && !ItemEquipped() && !_gameUIManager.usingUI)
        {
            if (!_canUseEquippedItem) return;
            _dialogueHandler.DisplayDetails("No item has been equipped");
        }  
        if (pokemonBitingPole && InputSourceHandler.InputPressed(ControlEvent.Confirm))
        {
            pokemonBitingPole = false;
            _encounterHandler.TriggerFishingEncounter(fishingTable,equippedSpecialItem);
        }
        if (fishing)
        {
            manager.ChangeAnimationState(PlayerAnimationState.FishingIdle);
            if (InputSourceHandler.InputPressed(ControlEvent.UseSpecialItem))
                ResetFishingAction();
        }
    }

    IEnumerator TryFishing()
    {
        var random = Utility.RandomRange(1, 11);
        yield return new WaitForSeconds(1f);
        if (!fishing) yield break;//if fishing canceled early
        if (random < 5)
        {
            pokemonBitingPole = true;
            _dialogueHandler.DisplayDetails("Oh!, a Bite!, Press Z");
            yield return new WaitForSeconds( (2 * (random/10f) ) + 1f);
            if (pokemonBitingPole)
            {
                _dialogueHandler.DisplayDetails("It got away");
                ResetFishingAction();
                yield return new WaitForSeconds(1);
                OnActionComplete?.Invoke();
            }
        }
        else
        {
            _dialogueHandler.DisplayDetails("Dang...nothing");
            ResetFishingAction();
            yield return new WaitForSeconds(1);
            OnActionComplete?.Invoke();
        }
    }
    public void PlayFishingAnimation()
    {
        manager.ChangeAnimationState(PlayerAnimationState.FishingStart);
        _dialogueHandler.DisplayDetails("fishing...");
    }
    private void StartFishingAction()
    {
        _playerMovementHandler.RestrictPlayerMovement();
        fishing = true;
        StartCoroutine(TryFishing());
    }
    public void ResetFishingAction()
    {
        fishing = false;
        pokemonBitingPole = false;
        manager.ChangeAnimationState(PlayerAnimationState.FishingEnd);
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
