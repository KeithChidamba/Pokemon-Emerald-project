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
    public List<Item> storeItems;
    public bool viewingStore;
    public Store_Item_ui[] storeItemsUI;
    public int numItems = 0;
    public int selectedItemIndex = 0;
    public int selectedItemQuantity = 0;
    public int topIndex = 0;
    public GameObject purchaseButton;
    public GameObject storeUI;
    public GameObject quantityUI;
    public Text quantity;    
    public Text playerMoneyText;
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
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            Save_manager.Instance.OnVirtualFileSystemLoaded += InitialiseItems;
        else
            InitialiseItems();
    }

    private void InitialiseItems()
    {        
        Save_manager.Instance.OnVirtualFileSystemLoaded -= InitialiseItems;
        storeItems.Clear();
        var itemPaths = Directory.GetFiles("Assets/Resources/Pokemon_project_assets/Player_obj/Bag/");
        List<Item> itemAssets = new(); 
        foreach (var itemPath in itemPaths)
        {
            if(Path.GetFileName(itemPath).Contains(".meta"))continue;
            
            var rawFilePath = itemPath.Split('.')[0];//remove extension  
            var itemName = Path.GetFileName(rawFilePath);
            var item = Resources.Load<Item>("Pokemon_project_assets/Player_obj/Bag/"+itemName);
            itemAssets.Add(item);
        }
        var orderedItems = itemAssets.OrderBy(item => item.price);
        foreach (var group in orderedItems.GroupBy(item => item.itemType).ToList())
        {
            foreach (var item in group)
                storeItems.Add(item);
        }
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
        purchaseButton.SetActive(true);
        quantityUI.SetActive(true);
    }
    public void NavigateDown()
    {
        if (topIndex < numItems - 7)
        {
            for (int i = 0; i < 6; i++)
            {
                storeItemsUI[i].item = storeItemsUI[i + 1].item;
            }
            storeItemsUI[6].item = storeItems[topIndex + 7];
            ReloadItems();
            topIndex++;
        }
    }
    public void NavigateUp()
    {
        if (topIndex > 0)
        {
            for (int i = 6; i > 0; i--)
            {
                storeItemsUI[i].item = storeItemsUI[i - 1].item;
            }
            storeItemsUI[0].item = storeItems[topIndex - 1];
            ReloadItems();
            topIndex--;
        }
    }
    public void BuyItem()
    {
        var item = Obj_Instance.CreateItem(storeItems[topIndex + selectedItemIndex - 1]);
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
                var priceOfItem = (selectedItemQuantity + 1) * storeItems[topIndex + selectedItemIndex - 1].price;
                if (Game_Load.Instance.playerData.playerMoney >= priceOfItem)
                    selectedItemQuantity += value;
                else
                    Dialogue_handler.Instance.DisplayInfo("Not enough money to buy that much!", "Details",1.5f);
            }
            else
                selectedItemQuantity = 99;
        }
    }
    public void ViewStore()
    {
        viewingStore = true;
        playerMoneyText.text = Game_Load.Instance.playerData.playerMoney.ToString();
        topIndex = 0;
        numItems = storeItems.Count;
        //if less than amount of ui elements, load that number, otherwise just load the first seven
        var numItemsForView = (numItems < 8) ? numItems : 7; 
        for (int i = 0; i < numItemsForView; i++)
        {
            storeItemsUI[i].item = storeItems[i];
            storeItemsUI[i].gameObject.SetActive(true);
            storeItemsUI[i].LoadItemUI();
        }
        SelectItem(1);
    }
    public void ExitStore()
    {
        selectedItemIndex = 0;
        purchaseButton.SetActive(false);
        quantityUI.SetActive(false);
        storeItemsUI[0].ResetUI();
        viewingStore = false;
    }
    void ReloadItems()
    {
        storeItemsUI[0].ResetUI();
        foreach (var item in storeItemsUI)
        {
            item.LoadItemUI();
        }
        SelectItem(1);
    }
}
