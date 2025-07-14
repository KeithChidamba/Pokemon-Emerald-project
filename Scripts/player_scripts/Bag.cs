using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Bag : MonoBehaviour
{
    public List<Item> bagItems;
    public Item_ui[] bagItemsUI;
    public int maxCapacity = 50;
    private int _numItems;
    public int selectedItem;
    public int topIndex;//keeps track of visible bag items
    public GameObject[] itemUIActions;
    public int sellQuantity = 1;
    public bool sellingItems;
    public GameObject sellingItemUI;
    public Text sellQuantityText;
    public TextMeshProUGUI itemUsageText;
    public static Bag Instance;
    public GameObject bagUI;
    public bool itemDroppable;
    public bool itemUsable;
    public bool itemGiveable;
    public GameObject itemSelector;
    public GameObject itemUsageSelector;
    public GameObject sellingIndicator;
    public List<GameObject> itemUsageUi;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Update()
    {
        if (sellingItems)
            sellQuantityText.text = "X" + sellQuantity;
    }
    private void SelectItem()
    {
        bagItemsUI[selectedItem].LoadItemDescription();
        itemUsageText.text = bagItemsUI[selectedItem].item.itemType=="Special" ? "Equip" : "Use";
        itemUsageText.fontSize = bagItemsUI[selectedItem].item.itemType=="Special" ? 20 : 24;
        if (sellingItems)
        {
            sellQuantity = 1;
            sellingItemUI.SetActive(true);
            foreach (var obj in itemUIActions)
                obj.SetActive(false);
        }
        else
        {
            sellingItemUI.SetActive(false);
            foreach (var obj in itemUIActions)
                obj.SetActive(true);
        }
    }
    
    public void SellToMarket()
    {
        var itemToSell = bagItems[topIndex + selectedItem];
        if (!itemToSell.canBeSold)
        {
            Dialogue_handler.Instance.DisplayDetails("You cant sell that!");
            return;
        }
        var price = itemToSell.price;
        var profit = (int)math.trunc((sellQuantity * price)/2f);
        Game_Load.Instance.playerData.playerMoney += profit;
        itemToSell.quantity -= sellQuantity;
        if (itemToSell.quantity == 0)
            RemoveItem();
        Dialogue_handler.Instance.DisplayList("You made P"+profit+ ", would you like to sell anything else?",
             "Sure, which item?", new[]{ "SellItem","LeaveStore" }, new[]{"Yes", "No"});
        InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.Bag);
    }
    public void CheckItemQuantity(Item item)
    {
        if (item.quantity > 0) return;
        bagItems.Remove(item);
        _numItems--;
    }
    public void ChangeQuantity(int value)
    {
        if (value < 0)//lower quantity
        {
            sellQuantity = (sellQuantity > 1)? sellQuantity + value : 1;
        }
        else if(value > 0)//increase quantity
        {
            var currentItem = bagItems[topIndex + selectedItem];
            sellQuantity = (sellQuantity < currentItem.quantity) ? sellQuantity + value : currentItem.quantity;
        }
    }
    public void NavigateDown()
    {
        if (topIndex < _numItems - 10 && selectedItem == 9)
        {
            for (int i = 0; i < 9; i++)
                bagItemsUI[i].item = bagItemsUI[i + 1].item;
            bagItemsUI[9].item = bagItems[topIndex + 10];
            ReloadItemUI();
            selectedItem = 8;
            topIndex++;
        }
        selectedItem++;
        selectedItem = Mathf.Clamp(selectedItem, 0, 9);
        SelectItem();

    }
    public void NavigateUp()
    {
        if (topIndex > 0 && selectedItem == 0)
        {
            for (int i = 9; i > 0; i--)
                bagItemsUI[i].item = bagItemsUI[i-1].item;
            bagItemsUI[0].item = bagItems[topIndex - 1];
            ReloadItemUI();
            selectedItem = 1;
            topIndex--;
        }
        selectedItem--;
        selectedItem = Mathf.Clamp(selectedItem, 0, 9);
        
        SelectItem();
    }
    public Item SearchForItem(string itemName)
    {
        return bagItems.FirstOrDefault(item => item.itemName == itemName & item.quantity < 99);
    }
    private bool BagContainsItem(string itemName)
    {
        return bagItems.Any(item => item.itemName == itemName);
    }

    public void AssignItemOptions(Item item)
    {
        itemDroppable = !Options_manager.Instance.playerInBattle && item.itemType!="Special";
        if (Options_manager.Instance.playerInBattle)
        {
            itemUsable = item.canBeUsedInBattle;
            itemGiveable = false;
        }
        else
        {
            itemUsable = item.canBeUsedInOverworld;
            if (item.isHeldItem)
                itemUsable = false;
            itemGiveable = item.canBeHeld;
        }
        ChangeImageVisibility(itemUsageUi[0],itemUsable);
        ChangeImageVisibility(itemUsageUi[1],itemGiveable);
        ChangeImageVisibility(itemUsageUi[2],itemDroppable);
    }

    void ChangeImageVisibility(GameObject imageObj, bool makeVisible)
    {
        var newTransparency = makeVisible ? 100 : 0;
        var color =  new Color(255,255,255,newTransparency);
        imageObj.GetComponent<Image>().color = color;
    }
    public void RemoveItem()
    {
        bagItems.Remove(bagItems[topIndex + selectedItem]);
        foreach (var itemUI in bagItemsUI)
            itemUI.gameObject.SetActive(false);
        bagItemsUI[0].ResetUI();
        foreach (GameObject i in itemUIActions)
            i.SetActive(false);
        ViewBag();
    }
    public void TakeItem(int memberIndex)
    {
        if (Options_manager.Instance.playerInBattle)
        {
            Dialogue_handler.Instance.DisplayDetails("Can't do that in battle",1f);
            return;
        }
        if (_numItems >= maxCapacity)
        {
            Dialogue_handler.Instance.DisplayDetails("Bag is full");
            return;
        }
        var partyMember = Pokemon_party.Instance.party[memberIndex - 1];
        Dialogue_handler.Instance.DisplayDetails("You took a " + partyMember.heldItem.itemName +" from "
                                             + partyMember.pokemonName);
        AddItem(partyMember.heldItem);
        partyMember.RemoveHeldItem();
        Pokemon_party.Instance.ClearSelectionUI();
        Pokemon_party.Instance.RefreshMemberCards();
    }
    public void GiveItem()
    {
        Pokemon_party.Instance.givingItem = true;
        Pokemon_party.Instance.ReceiveItem(bagItems[topIndex + selectedItem]);
        Game_ui_manager.Instance.ViewPokemonParty();
    } 
    public void UseItem()
     {
         var itemToUse = bagItems[topIndex + selectedItem];
         Item_handler.Instance.usingItem = true;
         if(itemToUse.forPartyUse)
         {
             Pokemon_party.Instance.ReceiveItem(itemToUse);
             Game_ui_manager.Instance.ViewPokemonParty();
         }
         else
             Item_handler.Instance.UseItem(itemToUse,null);
     }
    public void AddItem(Item item)
    {
        if (_numItems < maxCapacity)
        {
            if (BagContainsItem(item.itemName))
            {
                var itemFound = SearchForItem(item.itemName);
                if (itemFound != null)
                {
                    if ( item.quantity < (99 - itemFound.quantity))
                        itemFound.quantity += item.quantity;
                    else
                    {
                        var quantityGap = (99 - itemFound.quantity);
                        itemFound.quantity += quantityGap;
                        var overflow = Obj_Instance.CreateItem(item);
                        overflow.quantity = item.quantity - quantityGap;
                        bagItems.Add(overflow);
                        _numItems++;
                    }
                }
                else
                {
                    bagItems.Add(Obj_Instance.CreateItem(item));
                    _numItems++;
                }
            }
            else
            {
                bagItems.Add(Obj_Instance.CreateItem(item));
                _numItems++;
            }
        }
        else
            Dialogue_handler.Instance.DisplayDetails("Bag is full");
    }

    public void CloseBag()
    {
        selectedItem = 0;
        sellingItemUI.SetActive(false);
        sellingItems = false;
        foreach (var obj in itemUIActions)
            obj.SetActive(false);
        for (int i = 0; i < 10; i++)
            bagItemsUI[i].gameObject.SetActive(false);
        bagItemsUI[0].ResetUI();
    }
    public void ViewBag()
    {
        _numItems = 0; 
        _numItems = bagItems.Count;
        var numItemsForView = (_numItems < 11) ? _numItems : 10; 
        for (int i = 0; i < numItemsForView; i++)
        {
            bagItemsUI[i].item = bagItems[i];
            bagItemsUI[i].gameObject.SetActive(true);
            bagItemsUI[i].LoadItemUI();
        }
        selectedItem = 0;
        SelectItem();
    }
    void ReloadItemUI()
    {
        bagItemsUI[0].ResetUI();
        foreach (var item in bagItemsUI)
            item.LoadItemUI();
    }
}

