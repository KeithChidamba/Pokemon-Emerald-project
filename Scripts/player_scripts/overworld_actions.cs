using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class overworld_actions : MonoBehaviour,IInjectable
{
    public Animation_manager manager;

    public bool fishing;
    [SerializeField] private bool pokemonBitingPole;
    public bool doingAction;
    public bool usingUI;
    public Encounter_Area fishingArea;
    public Item equippedSpecialItem;
    public static overworld_actions Instance;
    private bool _canUseEquippedItem;
    private Equipable _currentEquippedItem;
    public event Action<Equipable> OnItemEquipped;
    public event Action<Equipable> OnItemUnequipped;
    private Dialogue_handler _dialogueHandler;
    private Player_movement _playerMovementHandler;
    private Encounter_handler _encounterHandler;
    private Game_Load _gameLoadingHandler;

    public void Inject(Container container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _encounterHandler = container.Resolve<Encounter_handler>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        gameObject.SetActive(true);
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        manager.OnFishingStart += StartFishingAction;
    }

    private void Start()
    {
        _canUseEquippedItem = false;
        _gameLoadingHandler.OnGameStarted += () => _canUseEquippedItem = true;
    }

    public void EquipItem(Item item)
    {
        if (item == null) return;//there was no item equipped in save data
        equippedSpecialItem = item;
        _currentEquippedItem = equippedSpecialItem.GetModule<EquipableInfoModule>().equipableItem;
        OnItemEquipped?.Invoke(_currentEquippedItem);
        if(usingUI)
            _dialogueHandler.DisplayDetails("Equipped " + equippedSpecialItem.itemName);
        _gameLoadingHandler.playerData.equippedItemName = equippedSpecialItem.itemName;
    }
    public bool IsEquipped(Equipable equipable = Equipable.None
        , Item item = null)
    {
        if (!_canUseEquippedItem || !ItemEquipped())
        {
            return false;
        }
        return item == null ? _currentEquippedItem == equipable 
            : _currentEquippedItem == item.GetModule<EquipableInfoModule>().equipableItem;
    }
    public void UnequipItem(Item item)
    {
        OnItemUnequipped?.Invoke(_currentEquippedItem);
        _currentEquippedItem = Equipable.None;
        equippedSpecialItem = null;
        if(usingUI)
            _dialogueHandler.DisplayDetails("Unequipped " + item.itemName);
        _gameLoadingHandler.playerData.equippedItemName = string.Empty;
    }
    public bool ItemEquipped()
    {
        return equippedSpecialItem != null;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && !ItemEquipped() && !usingUI)
        {
            if (!_canUseEquippedItem) return;
            _dialogueHandler.DisplayDetails("No item has been equipped");
        }  
        if (_dialogueHandler.displaying || usingUI || doingAction)
        {
            _playerMovementHandler.RestrictPlayerMovement();
        }
        if (pokemonBitingPole & Input.GetKeyDown(KeyCode.Z))
        {
            pokemonBitingPole = false;
            _encounterHandler.TriggerFishingEncounter(fishingArea,equippedSpecialItem);
            
        }
        if (fishing)
        {
            doingAction = true;
            manager.ChangeAnimationState(manager.fishingIdle);
            if (Input.GetKeyDown(KeyCode.C))
                ResetFishingAction();
        }
    }

    IEnumerator TryFishing()
    {
        var random = Utility.RandomRange(1, 11);
        yield return new WaitForSeconds(1f);
        if (!fishing) yield break;
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
                ActionReset();
            }
        }
        else
        {
            _dialogueHandler.DisplayDetails("Dang...nothing");
            ResetFishingAction();
            yield return new WaitForSeconds(1);
            ActionReset();
        }
    }
    private void StartFishingAction()
    {
        fishing = true;
        StartCoroutine(TryFishing());
    }
    public void ResetFishingAction()
    {
        fishing = false;
        pokemonBitingPole = false;
        ActionReset();
        manager.ChangeAnimationState(manager.fishingEnd);
    }

    public IEnumerator WaterTrees()
    {
        manager.ChangeAnimationState(manager.watering);
        _dialogueHandler.DisplayDetails("The tree is being watered");
        yield return new WaitForSeconds(2f);
        manager.ChangeAnimationState(manager.playerWalk);
    }
    void ActionReset()
    {
        doingAction = false;
        _playerMovementHandler.AllowPlayerMovement();
    }
}
