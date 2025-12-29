using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemUsage{Withdraw,Deposit,Toss,None}
public class ItemStorageHandler : MonoBehaviour
{
    public static ItemStorageHandler Instance;
    public ItemUsage currentUsage;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ViewItemsToWithdraw()
    {
        if (Bag.Instance.storageItems.Count==0)
        {
            Dialogue_handler.Instance.DisplayDetails("You have no items to withdraw");
            return;
        }
        currentUsage = ItemUsage.Withdraw;
        Bag.Instance.OnItemSelected += Bag.Instance.WithDrawFromStorage;
        Bag.Instance.currentBagUsage = BagUsage.SelectionOnly;
        Bag.Instance.storageView = true;
        Game_ui_manager.Instance.ViewBag();
    }
    public void OpenBagToDepositItem()
    {
        currentUsage = ItemUsage.Deposit;
        Bag.Instance.OnItemSelected += DepositItem;
        Bag.Instance.currentBagUsage = BagUsage.SelectionOnly;
        Game_ui_manager.Instance.ViewBag();
    }
    public void OpenBagToTossItem()
    {
        currentUsage = ItemUsage.Toss;
        Bag.Instance.OnItemSelected += TossItem;
        Bag.Instance.currentBagUsage = BagUsage.SelectionOnly;
        Game_ui_manager.Instance.ViewBag();
    }
    private void DepositItem(Item item)
    {
        Dialogue_handler.Instance.DisplayDetails("Sent "+item.itemName+" to storage");
        Bag.Instance.DepositToStorage(item);
    }
    private void TossItem(Item item)
    {
        Dialogue_handler.Instance.DisplayDetails("Threw "+item.itemName+(item.quantity==1?"":"'s ")+" away");
        Bag.Instance.RemoveItem(item);
    }
}

