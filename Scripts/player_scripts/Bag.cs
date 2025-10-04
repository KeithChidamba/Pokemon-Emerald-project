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
    public List<Item> allItems;
    public List<Item> currentCategoryOfItems;

    public enum BagCategory
    {
        General,Pokeballs,HmsTms,Berries,KeyItems
    };
    public int currentCategoryIndex;
    private BagCategory[] _categories;
    public Item_ui[] bagItemsUI;
    public int maxCapacity = 50;
    public int numItems;
    public int numItemsForView;
    public int selectedItemIndex;
    public int topIndex;//keeps track of visible bag items
    public GameObject[] itemUIActions;
    public int sellQuantity = 1;
    public enum BagUsage{NormalView,SellingView,SelectionOnly}
    public BagUsage currentBagUsage;
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
    public event Action<Item> OnItemSelected;
    public event Action OnBagOpened;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
    }

    private void Start()
    {
        _categories = (BagCategory[])Enum.GetValues(typeof(BagCategory));
    }

    private void Update()
    {
        if (currentBagUsage==BagUsage.SellingView)
            sellQuantityText.text = "X" + sellQuantity;
    }

    public void SelectItemForEvent()
    {
        OnItemSelected?.Invoke(currentCategoryOfItems[topIndex + selectedItemIndex]);
    }
    private void SelectItem()
    {
        bagItemsUI[selectedItemIndex].LoadItemDescription();
        var selectedItem = bagItemsUI[selectedItemIndex].item;
        if (selectedItem.itemType == Item_handler.ItemType.Special)
        {
            var equipDisplayText = overworld_actions.Instance.IsEquipped(item:selectedItem)? "Unequip":"Equip";
            itemUsageText.text = equipDisplayText;
            itemUsageText.fontSize = 18;
        }
        else
        {
            itemUsageText.text = "Use";
            itemUsageText.fontSize = 24;
        }

        if (currentBagUsage==BagUsage.SellingView) sellQuantity = 1;
    }
    
    public void SellToMarket()
    {
        var itemToSell = currentCategoryOfItems[topIndex + selectedItemIndex];
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
        if (item.quantity > 0)
        {
            bagItemsUI[currentCategoryOfItems.IndexOf(item)].LoadItemUI();
            return;
        }

        if (!Options_manager.Instance.playerInBattle)
        {
            InputStateHandler.Instance.ResetRelevantUi(new[] { InputStateHandler.StateName.PlayerBagItemUsage });
            InputStateHandler.Instance.OnStateChanged += state => state.currentSelectionIndex = 0;
        }
        RemoveItem(item);
    }
    public void ChangeQuantity(int value)
    {
        if (value < 0)//lower quantity
        {
            sellQuantity = (sellQuantity > 1)? sellQuantity + value : 1;
        }
        else if(value > 0)//increase quantity
        {
            var currentItem = currentCategoryOfItems[topIndex + selectedItemIndex];
            sellQuantity = (sellQuantity < currentItem.quantity) ? sellQuantity + value : currentItem.quantity;
        }
    }
    public void NavigateDown()
    {
        if (topIndex < numItems - 10 && selectedItemIndex == 9)
        {
            for (int i = 0; i < 9; i++)
                bagItemsUI[i].item = bagItemsUI[i + 1].item;
            bagItemsUI[9].item = currentCategoryOfItems[topIndex + 10];
            ReloadItemUI();
            selectedItemIndex = 8;
            topIndex++;
        }
        if (numItems == numItemsForView && selectedItemIndex == numItems-1)
            return;
        selectedItemIndex++;
        selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, 9);
        SelectItem();

    }
    public void NavigateUp()
    {
        if (topIndex > 0 && selectedItemIndex == 0)
        {
            for (int i = 9; i > 0; i--)
                bagItemsUI[i].item = bagItemsUI[i-1].item;
            bagItemsUI[0].item = currentCategoryOfItems[topIndex - 1];
            ReloadItemUI();
            selectedItemIndex = 1;
            topIndex--;
        }
        selectedItemIndex--;
        selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, 9);
        
        SelectItem();
    }

    public void ChangeCategoryLeft()
    {
        if (currentCategoryIndex > 0)
        {
            currentCategoryIndex--;
            ClearBagUI();
            ViewBag();
        }
    }
    public void ChangeCategoryRight()
    {
        if (currentCategoryIndex < _categories.Length-1)
        {
            currentCategoryIndex++;
            ClearBagUI();
            ViewBag();
        }
    }
    public Item SearchForItem(string itemName)
    {
        return allItems.FirstOrDefault(item => item.itemName == itemName & item.quantity < 99);
    }

    public void AssignItemOptions(Item item)
    {
        itemDroppable = !Options_manager.Instance.playerInBattle && item.itemType!=Item_handler.ItemType.Special;
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
        RemoveItem(allItems[topIndex + selectedItemIndex]);
    }
    private void RemoveItem(Item item)
    {
        allItems.Remove(item);
        ClearBagUI();
        ViewBag();
    }
    public void TakeItem(int memberIndex)
    {
        if (Options_manager.Instance.playerInBattle)
        {
            Dialogue_handler.Instance.DisplayDetails("Can't do that in battle",1f);
            return;
        }
        if (numItems >= maxCapacity)
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
        Pokemon_party.Instance.ReceiveItem(currentCategoryOfItems[topIndex + selectedItemIndex]);
        Game_ui_manager.Instance.ViewPokemonParty();
    } 
    public void UseItem()
     {
         var itemToUse = currentCategoryOfItems[topIndex + selectedItemIndex];
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
        if (numItems < maxCapacity)
        {
            if (allItems.Any(i=> i.itemName == item.itemName))
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
                        allItems.Add(overflow);
                        numItems++;
                    }
                }
                else
                {
                    allItems.Add(Obj_Instance.CreateItem(item));
                    numItems++;
                }
            }
            else
            {
                allItems.Add(Obj_Instance.CreateItem(item));
                numItems++;
            }
        }
        else
            Dialogue_handler.Instance.DisplayDetails("Bag is full");
    }

    void ClearBagUI()
    {
        foreach (var itemUI in bagItemsUI)
            itemUI.gameObject.SetActive(false);
        bagItemsUI[0].ResetUI();
        DisplayItemAction(false);
    }
    public void CloseBag()
    {
        selectedItemIndex = 0;
        sellingItemUI.SetActive(false);
        currentBagUsage = BagUsage.NormalView;
        ClearBagUI();
        OnItemSelected = null;
        OnBagOpened = null;
    }
    private List<Item> GetItems(Item_handler.ItemType itemType)
    {
        return allItems.Where(item => item.itemType == itemType).ToList();
    }
    public void ViewBag()
    {
        numItems = 0;
        topIndex = 0;
        
        var specialCategories = new[]
        {
            Item_handler.ItemType.Pokeball, Item_handler.ItemType.Special, Item_handler.ItemType.Berry,
            Item_handler.ItemType.LearnableMove
        };
        
        currentCategoryOfItems = _categories[currentCategoryIndex] switch
        {
            BagCategory.Pokeballs=>GetItems(specialCategories[0]),
            BagCategory.KeyItems=>GetItems(specialCategories[1]),
            BagCategory.Berries=>GetItems(specialCategories[2]),
            BagCategory.HmsTms=>GetItems(specialCategories[3]),
            _=>allItems.Where(item => !specialCategories.Contains(item.itemType)).ToList()
        };
        
        numItems = currentCategoryOfItems.Count;
        
        sellingItemUI.SetActive(currentBagUsage == BagUsage.SellingView);
        DisplayItemAction(currentBagUsage == BagUsage.NormalView);
        if (numItems == 0)
        {
            DisplayItemAction(false);
            return;
        }
        numItemsForView = (numItems < 11) ? numItems : 10; 
        for (int i = 0; i < numItemsForView; i++)
        {
            bagItemsUI[i].item = currentCategoryOfItems[i];
            bagItemsUI[i].gameObject.SetActive(true);
            bagItemsUI[i].LoadItemUI();
        }
        selectedItemIndex = 0;
        SelectItem();
        OnBagOpened?.Invoke();
    }
    void DisplayItemAction(bool display)
    {
        foreach (var obj in itemUIActions)
            obj.SetActive(display);
    }
    void ReloadItemUI()
    {
        bagItemsUI[0].ResetUI();
        foreach (var item in bagItemsUI)
            item.LoadItemUI();
    }
}

