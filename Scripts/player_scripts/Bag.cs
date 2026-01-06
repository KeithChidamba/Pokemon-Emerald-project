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
public enum BagUsage
{
    NormalView,
    SellingView,
    SelectionOnly
}
public class Bag : MonoBehaviour
{
    public List<Item> allItems;
    public List<Item> currentCategoryOfItems;
    public List<Item> storageItems;
    private enum BagCategory
    {
        General,Pokeballs,HmsTms,Berries,KeyItems
    };
    public int currentCategoryIndex;
    private BagCategory[] _categories;
    public Item_ui[] bagItemsUI;
    public int numItems;
    public int numItemsForView;
    public int selectedItemIndex;
    public int topIndex;//keeps track of visible bag items
    public int sellQuantity = 1;
    public int maxNumItemsForView;
    public int maxItemCapacity;

    public bool storageView;
    
    public BagUsage currentBagUsage;
    public GameObject sellingItemUI;
    public Text sellQuantityText;
    public Text sellingAmountText;
    public Text currentItemDescription;
    public Image currentItemImage;
    public Sprite[] bagCategoryImages;
    public Image currentBagImage;
    public Sprite[] bagCategoryTitles;
    public Image currentCategoryTitle;
    public GameObject[] bagCategoryIndicators;
    public static Bag Instance;
    public GameObject bagUI;
    public GameObject bagOverlayUI;
    public GameObject storageOverlayUI;
    public GameObject itemSelector;
    public LoopingUiAnimation[] redArrows;
    private LoopingUiAnimation _rightArrow;
    private LoopingUiAnimation _upArrow;
    private LoopingUiAnimation _downArrow;
    private LoopingUiAnimation _leftArrow;
    private int _totalSellingAmount;
    public event Action<Item> OnItemSelected;//bag managed
    public event Action<Item> OnItemUsed;//self-managed
    public event Action OnBagOpened;
    
    public event Action<Item> OnItemSold;

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
        _rightArrow=redArrows[0];
        _leftArrow=redArrows[1];
        _upArrow=redArrows[2];
        _downArrow=redArrows[3];
    }
    public void SelectItemForEvent()
    {
        OnItemSelected?.Invoke(currentCategoryOfItems[topIndex + selectedItemIndex]);
    }
    private void SelectItem()
    {
        bagItemsUI[selectedItemIndex].LoadItemDescription();
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
        
        Game_Load.Instance.playerData.playerMoney += _totalSellingAmount;
        itemToSell.quantity -= sellQuantity;
        if (itemToSell.quantity == 0)
            RemoveItem(itemToSell);
        Dialogue_handler.Instance.DisplayList("You made P"+_totalSellingAmount+ ", would you like to sell anything else?",
             "Sure, which item?", 
             new[]{ InteractionOptions.SellItem
                 ,InteractionOptions.LeaveStore }, new[]{"Yes", "No"});
        OnItemSold?.Invoke(itemToSell);
        InputStateHandler.Instance.ResetGroupUi(InputStateGroup.Bag);
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
            InputStateHandler.Instance.OnStateChanged += state => state.currentSelectionIndex = 0;
        }
        RemoveItem(item);
    }

    public void ResetItemSellingUi()
    {
        sellQuantity = 1;
        sellQuantityText.text = "X0";
        sellingAmountText.text = "0";
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
        _totalSellingAmount = (int)math.trunc((sellQuantity 
                                               * currentCategoryOfItems[topIndex + selectedItemIndex].price)/ 2f);
        sellQuantityText.text = "X" + sellQuantity;
        sellingAmountText.text = _totalSellingAmount.ToString();
    }
    public void NavigateDown()
    {
        if (numItems == 0) return;
        if (numItemsForView == 1) return;
        if (topIndex < numItems - maxNumItemsForView && selectedItemIndex == maxNumItemsForView - 1)
        {
            for (int i = 0; i < maxNumItemsForView - 1; i++)
                bagItemsUI[i].item = bagItemsUI[i + 1].item;

            bagItemsUI[maxNumItemsForView - 1].item = currentCategoryOfItems[topIndex + maxNumItemsForView];
            ReloadItemUI();
            selectedItemIndex = maxNumItemsForView - 2;
            topIndex++;
        }
        
        if (numItems == numItemsForView && selectedItemIndex == numItems - 1)
            return;

        selectedItemIndex++;
        selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, maxNumItemsForView - 1);

        _upArrow.gameObject.SetActive(selectedItemIndex > 0);
        _downArrow.gameObject.SetActive(selectedItemIndex < numItems - 1);

        SelectItem();
    }
    public void NavigateUp()
    {
        if (numItems == 0) return;
        if (numItemsForView == 1) return;
        if (topIndex > 0 && selectedItemIndex == 0)
        {
            for (int i = maxNumItemsForView-1; i > 0; i--)
                bagItemsUI[i].item = bagItemsUI[i-1].item;
            bagItemsUI[0].item = currentCategoryOfItems[topIndex - 1];
            ReloadItemUI();
            selectedItemIndex = 1;
            topIndex--;
        }
        selectedItemIndex--;
        _downArrow.gameObject.SetActive(true);
        _upArrow.gameObject.SetActive(selectedItemIndex>0);
        selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, maxNumItemsForView-1);
        
        SelectItem();
    }

    public void ChangeCategoryLeft()
    {
        if (storageView) return;
        if (currentCategoryIndex > 0)
        {
            bagCategoryIndicators[currentCategoryIndex].SetActive(false);
            currentCategoryIndex--;
            bagCategoryIndicators[currentCategoryIndex].SetActive(true);
            currentCategoryTitle.sprite = bagCategoryTitles[currentCategoryIndex];
            currentBagImage.sprite = bagCategoryImages[currentCategoryIndex];
            ClearBagUI();
            ViewBag();
        }
        _rightArrow.gameObject.SetActive(true);
        _leftArrow.gameObject.SetActive(currentCategoryIndex>0);
    }
    public void ChangeCategoryRight()
    {
        if (storageView) return;
        if (currentCategoryIndex < _categories.Length-1)
        {
            bagCategoryIndicators[currentCategoryIndex].SetActive(false);
            currentCategoryIndex++;
            bagCategoryIndicators[currentCategoryIndex].SetActive(true);
            currentCategoryTitle.sprite = bagCategoryTitles[currentCategoryIndex];
            currentBagImage.sprite = bagCategoryImages[currentCategoryIndex];
            ClearBagUI();
            ViewBag();
        }        
        _leftArrow.gameObject.SetActive(true);
        _rightArrow.gameObject.SetActive(currentCategoryIndex!=_categories.Length-1);
    }
    public Item SearchForItem(string itemName)
    {
        return allItems.FirstOrDefault(item => item.itemName == itemName & item.quantity < 99);
    }
    
    public void RemoveItem(Item item)
    {
        allItems.Remove(item);
        ClearBagUI();
        ViewBag();
    }
    public void TakeItem(int memberIndex)
    {
        if (Options_manager.Instance.playerInBattle)
        {
            Dialogue_handler.Instance.DisplayDetails("Can't do that in battle");
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
    public void OpenBagToGiveItem()
    {
        if (Options_manager.Instance.playerInBattle)
        {
            Dialogue_handler.Instance.DisplayDetails("Can't do that in battle");
            return;
        }
        currentBagUsage = BagUsage.SelectionOnly;
        OnItemSelected += GiveItem;
        Game_ui_manager.Instance.ViewBag();
    }

    private void GiveItem(Item itemToBeGiven)
    {
        if (!itemToBeGiven.canBeHeld)
        {
            Dialogue_handler.Instance.DisplayDetails("Pokemon can't hold that item");
            return;
        }
        var partyMember = Pokemon_party.Instance.party[Pokemon_party.Instance.selectedMemberNumber-1];
        InputStateHandler.Instance.ResetRelevantUi(new[] { InputStateName.PokemonPartyOptions });
        InputStateHandler.Instance.ResetGroupUi(InputStateGroup.Bag);
        
        Dialogue_handler.Instance.DisplayDetails(partyMember.pokemonName
                                                 +" received a "+itemToBeGiven.itemName);
        
        partyMember.GiveItem(Obj_Instance.CreateItem(itemToBeGiven));
        itemToBeGiven.quantity--;
        CheckItemQuantity(itemToBeGiven);
        Pokemon_party.Instance.RefreshMemberCards();
    }
    public void UseItem()
     {
         var itemToUse = currentCategoryOfItems[topIndex + selectedItemIndex];
         OnItemUsed?.Invoke(itemToUse);
         if (Options_manager.Instance.playerInBattle)
         {
             if (!itemToUse.canBeUsedInBattle)
             {
                 Dialogue_handler.Instance.DisplayDetails("Can't use that in battle");
                 return;
             }
         }
         else 
         {
             if (!itemToUse.canBeUsedInOverworld) 
             {
                 Dialogue_handler.Instance.DisplayDetails("Can't use that in battle");
                 return;//special items for events
             }
         }
         Item_handler.Instance.usingItem = true;
         if(itemToUse.forPartyUse)
         {
             Pokemon_party.Instance.ReceiveItem(itemToUse);
             Game_ui_manager.Instance.ViewPokemonParty();
         }
         else
             Item_handler.Instance.UseItem(itemToUse,null);
     }

    public void WithDrawFromStorage(Item itemToWithdraw)
    {
        Dialogue_handler.Instance.DisplayDetails("withdrew "+itemToWithdraw.itemName);
        AddItem(itemToWithdraw);
        storageItems.Remove(itemToWithdraw);
        ClearBagUI();
        ViewBag();
    }
    public void DepositToStorage(Item itemToDeposit)
    {
        storageItems.Add(itemToDeposit);
        RemoveItem(itemToDeposit);
    }
    public void AddItem(Item item)
    {
        if (allItems.Any(i=> i.itemName == item.itemName))
        {
            var itemFound = SearchForItem(item.itemName);
            if (itemFound != null)
            {
                if ( item.quantity < (maxItemCapacity - itemFound.quantity))
                    itemFound.quantity += item.quantity;
                else
                {
                    var quantityGap = (maxItemCapacity - itemFound.quantity);
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

    void ClearBagUI()
    {
        foreach (var itemUI in bagItemsUI)
            itemUI.gameObject.SetActive(false);
        bagItemsUI[0].ResetUI();
    }
    public void CloseBag()
    {
        storageView = false;
        selectedItemIndex = 0;
        bagCategoryIndicators[currentCategoryIndex].SetActive(false);
        currentCategoryIndex = 0;
        currentCategoryTitle.sprite = bagCategoryTitles[currentCategoryIndex];
        currentBagImage.sprite = bagCategoryImages[currentCategoryIndex];
        sellingItemUI.SetActive(false);
        currentBagUsage = BagUsage.NormalView;
        ItemStorageHandler.Instance.currentUsage = ItemUsage.None;
        ClearBagUI();
        foreach (var loopingUiAnimation in redArrows)
        {
            loopingUiAnimation.viewingUI = false;
            loopingUiAnimation.gameObject.SetActive(true);
        }
        InputStateHandler.Instance.ResetRelevantUi(InputStateName.ItemStorageUsage);
        OnItemSelected = null;
        OnBagOpened = null;
        
    }
    private List<Item> GetItems(ItemType itemType)
    {
        return allItems.Where(item => item.itemType == itemType).ToList();
    }
    public void ViewBag()
    {
        numItems = 0;
        topIndex = 0;
        
        var specialCategories = new[]
        {
            ItemType.Pokeball, ItemType.Special, ItemType.Berry,
            ItemType.LearnableMove
        };
        
        currentCategoryOfItems = storageView? storageItems 
            : _categories[currentCategoryIndex] switch
        {
            BagCategory.Pokeballs=>GetItems(specialCategories[0]),
            BagCategory.KeyItems=>GetItems(specialCategories[1]),
            BagCategory.Berries=>GetItems(specialCategories[2]),
            BagCategory.HmsTms=>GetItems(specialCategories[3]),
            _=>allItems.Where(item => !specialCategories.Contains(item.itemType)).ToList()
        };
        
        numItems = currentCategoryOfItems.Count;
        if (numItems == 0)
        {
            if(ItemStorageHandler.Instance.currentUsage == ItemUsage.Deposit)
            {
                OnBagOpened = null;
                Dialogue_handler.Instance.DisplayDetails("You have no items to deposit");
                InputStateHandler.Instance.RemoveTopInputLayer(true);
                return;
            }
            if (storageView)
            {
                OnBagOpened = null;
                Dialogue_handler.Instance.DisplayDetails("You have no items to withdraw");
                InputStateHandler.Instance.RemoveTopInputLayer(true);
                return;
            }
        }
        sellingItemUI.SetActive(currentBagUsage == BagUsage.SellingView);
        numItemsForView = (numItems < maxNumItemsForView+1) ? numItems : maxNumItemsForView; 
        for (int i = 0; i < numItemsForView; i++)
        {
            bagItemsUI[i].item = currentCategoryOfItems[i];
            bagItemsUI[i].gameObject.SetActive(true);
            bagItemsUI[i].LoadItemUI();
        }
        selectedItemIndex = 0;
        
        if (numItems > 0) SelectItem();
        if (InputStateHandler.Instance.currentState.stateGroups.Contains(InputStateGroup.Bag))
        {
            InputStateHandler.Instance.PlayerBagNavigationRestrictions();
        }

        OnBagOpened?.Invoke();
        
        //default visuals
        _upArrow.gameObject.SetActive(false);
        _downArrow.gameObject.SetActive(numItems>1);
        _leftArrow.gameObject.SetActive(false);
        foreach (var loopingUiAnimation in redArrows)
        {
            loopingUiAnimation.viewingUI = true;
        }
    }

    void ReloadItemUI()
    {
        bagItemsUI[0].ResetUI();
        foreach (var item in bagItemsUI)
            item.LoadItemUI();
    }
}



