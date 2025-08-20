using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class overworld_actions : MonoBehaviour
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
    private EquipableItemInfo.Equipable _currentEquippedItem;
    public event Action<EquipableItemInfo.Equipable> OnItemEquipped;
    public event Action<EquipableItemInfo.Equipable> OnItemUnequipped;
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
        Game_Load.Instance.OnGameStarted += () => _canUseEquippedItem = true;
    }

    public void EquipItem(Item item)
    {
        if (item == null) return;//there was no item equipped in save data
        equippedSpecialItem = item;
        _currentEquippedItem = equippedSpecialItem.GetModule<EquipableItemInfo>().equipableItem;
        OnItemEquipped?.Invoke(_currentEquippedItem);
        if(usingUI)
            Dialogue_handler.Instance.DisplayDetails("Equipped " + equippedSpecialItem.itemName, 1f);
        Game_Load.Instance.playerData.equippedItemName = equippedSpecialItem.itemName;
    }
    public bool IsEquipped(EquipableItemInfo.Equipable equipable = EquipableItemInfo.Equipable.None
        , Item item = null)
    {
        if (!_canUseEquippedItem || !ItemEquipped())
        {
            return false;
        }
        return item == null ? _currentEquippedItem == equipable 
            : _currentEquippedItem == item.GetModule<EquipableItemInfo>().equipableItem;
    }
    public void UnequipItem(Item item)
    {
        OnItemUnequipped?.Invoke(_currentEquippedItem);
        _currentEquippedItem = EquipableItemInfo.Equipable.None;
        equippedSpecialItem = null;
        if(usingUI)
            Dialogue_handler.Instance.DisplayDetails("Unequipped " + item.itemName, 1f);
        Game_Load.Instance.playerData.equippedItemName = string.Empty;
    }
    private bool ItemEquipped()
    {
        return equippedSpecialItem != null;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && !ItemEquipped() && !usingUI)
        {
            if (!_canUseEquippedItem) return;
            Dialogue_handler.Instance.DisplayDetails("No item has been equipped", 2f);
        }  
        if (usingUI)
        {
            Player_movement.Instance.RestrictPlayerMovement();
            return;
        }
        if (doingAction)
            Player_movement.Instance.RestrictPlayerMovement();
        
        if (pokemonBitingPole & Input.GetKeyDown(KeyCode.Z))
        {
            pokemonBitingPole = false;
            Encounter_handler.Instance.TriggerFishingEncounter(fishingArea,equippedSpecialItem);
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
            Dialogue_handler.Instance.DisplayDetails("Oh!, a Bite!, Press F");
            yield return new WaitForSeconds( (2 * (random/10f) ) + 1f);
            if (pokemonBitingPole)
            {
                Dialogue_handler.Instance.DisplayDetails("It got away",1.5f);
                ResetFishingAction();
                yield return new WaitForSeconds(1);
                ActionReset();
            }
        }
        else
        {
            Dialogue_handler.Instance.DisplayDetails("Dang...nothing",1.5f);
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
        manager.ChangeAnimationState(manager.fishingEnd);
    }
    void ActionReset()
    {
        doingAction = false;
        Player_movement.Instance.AllowPlayerMovement();
    }
}
