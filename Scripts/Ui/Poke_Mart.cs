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
    public List<Item> allItems;
    public bool viewingStore;
    public Store_Item_ui[] storeItemsUI;
    public int numItems = 0;
    public int selectedItemIndex = 0;
    public int selectedItemQuantity = 0;
    public int topIndex = 0;
    public GameObject storeUI;
    public GameObject quantityUI;
    public Text quantity;    
    public Text playerMoneyText;
    public PokeMartData currentMartData;
    private bool _itemsLoaded;
    public GameObject itemSelector;
    public GameObject quantitySelector;
    public static Poke_Mart Instance;
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
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            Save_manager.Instance.OnVirtualFileSystemLoaded += InitialiseItems;
        else
            InitialiseItems();
    }

    private void InitialiseItems()
    {        
        Save_manager.Instance.OnVirtualFileSystemLoaded -= InitialiseItems;
        allItems = Resources.LoadAll<Item>("Pokemon_project_assets/Items/Mart_Items").ToList(); 
    }
    
    private IEnumerator SelectItemsForStore()
    {
        currentStoreItems.Clear();
        List<Item> validItems=new();
        foreach (var item in allItems)
        {
            if(currentMartData.availableItems.Contains(item.itemName.ToLower()))
                validItems.Add(item);
        }
        var orderedItems = validItems.OrderBy(item => item.price);
        var itemProgress = 0;
        foreach (var group in orderedItems.GroupBy(item => item.itemType).ToList())
        {
            foreach (var item in group)
            {
                currentStoreItems.Add(item);
                itemProgress++;
            }
        }
        yield return new WaitUntil(() => itemProgress == validItems.Count());
        _itemsLoaded = true;
    }
    private void Update()
    {
        if (!viewingStore) return;
        quantity.text = selectedItemQuantity.ToString();
        playerMoneyText.text = Game_Load.Instance.playerData.playerMoney.ToString();
    }
    public void SelectItem(int itemPos)
    {
        selectedItemIndex = itemPos;
        selectedItemQuantity = 1;
        storeItemsUI[selectedItemIndex - 1].LoadItemDescription();
    }
    public void NavigateDown()
    {
        if (topIndex < numItems - 7)// && selectedItemIndex == 7)
        {
            for (int i = 0; i < 6; i++)
                storeItemsUI[i].item = storeItemsUI[i + 1].item;
            
            storeItemsUI[6].item = currentStoreItems[topIndex + 7];
            ReloadItems();
            topIndex++;
        }
        // selectedItemIndex++;
        // selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, 7);
        // SelectItem(selectedItemIndex);
    }
    public void NavigateUp()
    {
        if (topIndex > 0 )//&& selectedItemIndex == 0)
        {
            for (int i = 6; i > 0; i--)
                storeItemsUI[i].item = storeItemsUI[i - 1].item;
            
            storeItemsUI[0].item = currentStoreItems[topIndex - 1];
            ReloadItems();
            topIndex--;
        }
        // selectedItemIndex--;
        // selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, 7);
        // SelectItem(selectedItemIndex);
    }
    public void BuyItem()
    {
        var item = Obj_Instance.CreateItem(currentStoreItems[topIndex + selectedItemIndex - 1]);
        if(Game_Load.Instance.playerData.playerMoney >= item.price)
        {
            item.quantity = selectedItemQuantity;
            Bag.Instance.AddItem(item);
            Game_Load.Instance.playerData.playerMoney -= selectedItemQuantity * item.price;
            Dialogue_handler.Instance.DisplayInfo("You bought "+ item.quantity+ " "+item.itemName+"'s", "Details",1.2f);
            selectedItemQuantity = 1;
        }
        else
            Dialogue_handler.Instance.DisplayInfo("You dont have enough money for that!", "Details",1.5f);
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
                var priceOfItem = (selectedItemQuantity + 1) * currentStoreItems[topIndex + selectedItemIndex - 1].price;
                if (Game_Load.Instance.playerData.playerMoney >= priceOfItem)
                    selectedItemQuantity += value;
                else
                    Dialogue_handler.Instance.DisplayInfo("Not enough money to buy that much!", "Details",1.5f);
            }
            else
                selectedItemQuantity = 99;
        }
    }
    private void ViewStore(Overworld_interactable clerkInteractable)
    {
        if (clerkInteractable.interactionType != "Clerk") return; //Mart Location
       
        foreach (var data in Resources.LoadAll<PokeMartData>("Pokemon_project_assets/Overwolrd_obj/Poke_Mart_Data"))
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
        viewingStore = true;
        playerMoneyText.text = Game_Load.Instance.playerData.playerMoney.ToString();
        topIndex = 0;
        numItems = currentStoreItems.Count;
        //if less than amount of ui elements, load that number, otherwise just load the first seven
        var numItemsForView = (numItems < 8) ? numItems : 7; 
        for (int i = 0; i < numItemsForView; i++)
        {
            storeItemsUI[i].item = currentStoreItems[i];
            storeItemsUI[i].gameObject.SetActive(true);
            storeItemsUI[i].LoadItemUI();
        }
        SelectItem(1);
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
        SelectItem(1);
    }
}
