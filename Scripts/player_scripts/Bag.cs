using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Bag : MonoBehaviour
{
    public List<Item> bagItems;
    public bool viewingBag;
    public Item_ui[] bagItemsUI;
    public int maxCapacity = 50;
    public int numItems;
    public int selectedItem;
    public int topIndex;//keeps track of visible bag items
    public GameObject[] itemUIActions;
    public int sellQuantity = 1;
    public bool sellingItems;
    public GameObject sellingItemUI;
    public Text sellQuantityText;
    public static Bag Instance;
    public GameObject bagUI;
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
    public void SelectItem(int itemPosition)
    {
        selectedItem = itemPosition - 1;
        bagItemsUI[selectedItem].LoadItemDescription();
        if (sellingItems)
        {
            sellQuantity = 1;
            sellingItemUI.SetActive(true);
        }
        else
        {
            foreach (var obj in itemUIActions)
                obj.SetActive(true);
        }
    }
    
    public void SellToMarket()
    {
        var itemToSell = bagItems[topIndex + selectedItem - 1];
        if (!itemToSell.canBeSold)
        {
            Dialogue_handler.instance.Write_Info("You cant sell that!","Details");
            return;
        }
        var price = itemToSell.price;
        var profit = (int)math.trunc((sellQuantity * price)/2f);
        Game_Load.Instance.playerData.playerMoney += profit;
        itemToSell.quantity -= sellQuantity;
        if (itemToSell.quantity == 0)
            RemoveItem();
        Dialogue_handler.instance.Write_Info("You made P"+profit+ ", would you like to sell anything else?", "Options", "Sell_item","Sure, which item?","Dont_Buy","Yes","No");
        Game_ui_manager.Instance.CloseBag();
    }

    public void CheckItemQuantity(Item item)
    {
        if (item.quantity > 0) return;
        bagItems.Remove(item);
        numItems--;
    }
    public void ChangeQuantity(int value)
    {
        if (value < 0)//lower quantity
        {
            sellQuantity = (sellQuantity > 1)? sellQuantity + value : 1;
        }
        else if(value > 0)//increase quantity
        {
            var currentItem = bagItems[topIndex + selectedItem - 1];
            sellQuantity = (sellQuantity < currentItem.quantity) ? sellQuantity + value : currentItem.quantity;
        }
    }
    public void NavigateDown()
    {
        if (topIndex < numItems-10)
        {
            for (int i = 0; i < 9; i++)
                bagItemsUI[i].item = bagItemsUI[i + 1].item;  
            bagItemsUI[9].item = bagItems[topIndex + 10];
            ReloadItemUI();
            topIndex++;
        }

    }
    public void NavigateUp()
    {
        if (topIndex > 0)
        {
            for (int i = 9; i > 0; i--)
                bagItemsUI[i].item = bagItemsUI[i-1].item;
            bagItemsUI[0].item = bagItems[topIndex - 1];
            ReloadItemUI();
            topIndex--;
        }
    }
    private Item SearchForItem(string itemName)
    {
        return bagItems.FirstOrDefault(item => item.itemName == itemName & item.quantity < 99);
    }
    private bool BagContainsItem(string itemName)
    {
        return bagItems.Any(item => item.itemName == itemName);
    }
    public void RemoveItem()
    {
        bagItems.Remove(bagItems[topIndex + selectedItem - 1]);
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
            Dialogue_handler.instance.Write_Info("Can't do that in battle", "Details",1f);
            return;
        }
        if (numItems >= maxCapacity)
        {
            Dialogue_handler.instance.Write_Info("Bag is full", "Details");
            return;
        }
        var partyMember = Pokemon_party.Instance.party[memberIndex - 1];
        Dialogue_handler.instance.Write_Info("You took a " + partyMember.HeldItem.itemName +" from "
                                             + partyMember.Pokemon_name, "Details");
        AddItem(partyMember.HeldItem);
        partyMember.RemoveHeldItem();
        Pokemon_party.Instance.ClearSelectionUI();
        Pokemon_party.Instance.RefreshMemberCards();
    }
    public void GiveItem()
    {
        Pokemon_party.Instance.givingItem = true;
        Pokemon_party.Instance.ReceiveItem(bagItems[topIndex + selectedItem - 1]);
        Game_ui_manager.Instance.CloseBag();
        Game_ui_manager.Instance.ViewPokemonParty();
    }
    public void AddItem(Item item)
    {
        if (numItems < maxCapacity)
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
                        numItems++;
                    }
                }
                else
                {
                    bagItems.Add(Obj_Instance.CreateItem(item));
                    numItems++;
                }
            }
            else
            {
                bagItems.Add(Obj_Instance.CreateItem(item));
                numItems++;
            }
        }
        else
        {
            if(Poke_Mart.instance.viewing_store)
                Game_ui_manager.Instance.CloseStore();
            Dialogue_handler.instance.Write_Info("Bag is full", "Details");
        }                                                                           
    }
    public void UseItem()
    {
        var itemToUse = bagItems[topIndex + selectedItem - 1];
        Item_handler.Instance.usingItem = true;
        if(itemToUse.forPartyUse)
        {
            Pokemon_party.Instance.ReceiveItem(itemToUse);
            Game_ui_manager.Instance.ViewPokemonParty();
        }
        else
            Item_handler.Instance.UseItem(itemToUse);
        Game_ui_manager.Instance.CloseBag();
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
        numItems = 0; 
        viewingBag = true; 
        numItems = bagItems.Count;
        var numItemsForView = (numItems < 11) ? numItems : 10; 
        for (int i = 0; i < numItemsForView; i++)
        {
            bagItemsUI[i].item = bagItems[i];
            bagItemsUI[i].gameObject.SetActive(true);
            bagItemsUI[i].LoadItemUI();
        }
    }
    void ReloadItemUI()
    {
        bagItemsUI[0].ResetUI();
        foreach (Item_ui itm in bagItemsUI)
            itm.LoadItemUI();
    }
}

