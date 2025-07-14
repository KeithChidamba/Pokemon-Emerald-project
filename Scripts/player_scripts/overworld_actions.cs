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

    public void EquipItem(Item item)
    {
        if (item == null) return;//there was no item equipped in save data
        equippedSpecialItem = item;
        if(usingUI)
            Dialogue_handler.Instance.DisplayDetails("equipped " + equippedSpecialItem.itemName, 1f);
        Game_Load.Instance.playerData.equippedItemName = equippedSpecialItem.itemName;
    }
    public bool IsEquipped(string keyword)
    {
        if (!ItemEquipped())
        {//prevent null check
            return false;
        }
        return equippedSpecialItem.itemName.ToLower().Contains(keyword.ToLower());
    }

    private bool ItemEquipped()
    {
        return equippedSpecialItem != null;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && !ItemEquipped() && !usingUI)
        {
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
