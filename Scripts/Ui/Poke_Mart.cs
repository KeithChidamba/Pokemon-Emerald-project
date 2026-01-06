using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Poke_Mart : MonoBehaviour
{
    public List<Item> currentStoreItems;
    public bool viewingStore;
    public Store_Item_ui[] storeItemsUI;
    public int numItems = 0;
    public int numItemsForView;
    public int selectedItemIndex = 0;
    public int selectedItemQuantity = 0;
    public int numDisplayableItems=7;
    public int topIndex = 0;
    public GameObject storeUI;
    public GameObject quantityUI;
    public Text quantity;    
    public Text playerMoneyText;
    public PokeMartData currentMartData;
    private bool _itemsLoaded;
    public GameObject itemSelector;
    public GameObject quantitySelector;
    public Text itemDescription;
    public static Poke_Mart Instance;
    public event Action<Item> OnItemBought;
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
        Options_manager.Instance.OnInteractionTriggered += ViewStore;
    }
    private IEnumerator SelectItemsForStore()
    {
        currentStoreItems.Clear();
        var orderedItems = currentMartData.availableItems.OrderBy(item => item.price);
        var itemProgress = 0;
        foreach (var group in orderedItems.GroupBy(item => item.itemType).ToList())
        {
            foreach (var item in group)
            {
                currentStoreItems.Add(item);
                itemProgress++;
            }
        }
        yield return new WaitUntil(() => itemProgress == currentMartData.availableItems.Count);
        _itemsLoaded = true;
    }
    private void Update()
    {
        if (!viewingStore) return;
        quantity.text = selectedItemQuantity.ToString();
        playerMoneyText.text = Game_Load.Instance.playerData.playerMoney.ToString();
    }
    private void SelectItem()
    {
        selectedItemQuantity = 1;
        storeItemsUI[selectedItemIndex].LoadItemDescription();
    }
    public void NavigateDown()
    {
        if (topIndex < numItems - numDisplayableItems && selectedItemIndex == numDisplayableItems-1)
        {
            for (int i = 0; i < numDisplayableItems-1; i++)
                storeItemsUI[i].item = storeItemsUI[i + 1].item;
            
            storeItemsUI[numDisplayableItems-1].item = currentStoreItems[topIndex + numDisplayableItems];
            selectedItemIndex = numDisplayableItems-2;
            ReloadItems();
            topIndex++;
        }

        if (numItems == numItemsForView && selectedItemIndex == numItems-1)
            return;
        
        selectedItemIndex++;
        selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, numDisplayableItems-1);
        SelectItem();
    }
    public void NavigateUp()
    {
        if (topIndex > 0 && selectedItemIndex == 0)
        {
            for (int i = numDisplayableItems-1; i > 0; i--)
                storeItemsUI[i].item = storeItemsUI[i - 1].item;
            
            storeItemsUI[0].item = currentStoreItems[topIndex - 1];
            ReloadItems();
            selectedItemIndex = 1;
            topIndex--;
        }
        selectedItemIndex--;
        selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, numDisplayableItems-1);
        SelectItem();
    }
    public void BuyItem()
    {
        var item = Obj_Instance.CreateItem(currentStoreItems[topIndex + selectedItemIndex]);
        if(Game_Load.Instance.playerData.playerMoney >= item.price)
        {
            item.quantity = selectedItemQuantity;
            Bag.Instance.AddItem(item);
            Game_Load.Instance.playerData.playerMoney -= selectedItemQuantity * item.price;
            Dialogue_handler.Instance.DisplayDetails("You bought "+ item.quantity+ " "+item.itemName+"'s");
            selectedItemQuantity = 1;
            OnItemBought?.Invoke(item);
        }
        else
            Dialogue_handler.Instance.DisplayDetails("You dont have enough money for that!");
    }
    public void ChangeItemQuantity(int value)
    {
        if (value < 0)//lower quantity
        {
            selectedItemQuantity = (selectedItemQuantity > 1)? selectedItemQuantity + value : 1;
        }
        else if(value > 0)//increase quantity
        {
            if (selectedItemQuantity < 99)//below max quantity and affordable by player
            {
                var priceOfItem = (selectedItemQuantity + 1) * currentStoreItems[topIndex + selectedItemIndex].price;
                if (Game_Load.Instance.playerData.playerMoney >= priceOfItem)
                    selectedItemQuantity += value;
                else
                    Dialogue_handler.Instance.DisplayDetails("Not enough money to buy that much!");
            }
            else
                selectedItemQuantity = 99;
        }
    }
    private void ViewStore(Overworld_interactable clerkInteractable, int optionChosen)
    {
        if (clerkInteractable.interactionType != InteractionType.Clerk) return;
        Dialogue_handler.Instance.EndDialogue();
        
        if (optionChosen > 0) return;
        
        if(currentMartData!=null){
            if (currentMartData.location == clerkInteractable.location)
            {//basically caching
                SetUpItemView();
                return;
            }
        }
        
        var allData = Resources.LoadAll<PokeMartData>(
            Save_manager.GetDirectory(AssetDirectory.PokeMartData));
        
        foreach (var data in allData)
        {
            if (data.location == clerkInteractable.location)
            {
                currentMartData = data;
                StartCoroutine(InitializeStoreData());
                break;
            }
        }
    }
    private IEnumerator InitializeStoreData()
    {
        _itemsLoaded = false;
        StartCoroutine(SelectItemsForStore());
        yield return new WaitUntil(()=>_itemsLoaded);
        SetUpItemView();
    }
    private void SetUpItemView()
    {
        viewingStore = true;
        playerMoneyText.text = Game_Load.Instance.playerData.playerMoney.ToString();
        topIndex = 0;
        numItems = currentStoreItems.Count;
        //if less than amount of ui elements, load that number, otherwise just load the first seven
        numItemsForView = (numItems < numDisplayableItems+1) ? numItems : numDisplayableItems; 
        for (int i = 0; i < numItemsForView; i++)
        {
            storeItemsUI[i].item = currentStoreItems[i];
            storeItemsUI[i].gameObject.SetActive(true);
            storeItemsUI[i].LoadItemUI();
        }
        SelectItem();
        Game_ui_manager.Instance.ViewPokeMart();
    }
    public void ExitStore()
    {
        selectedItemIndex = 0;
        quantityUI.SetActive(false);
        foreach (var item in storeItemsUI)
            item.ClearUI();
        viewingStore = false;
    }
    void ReloadItems()
    {
        foreach (var item in storeItemsUI)
            item.LoadItemUI();
        SelectItem();
    }
}
