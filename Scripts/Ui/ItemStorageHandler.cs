using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemStorageHandler : MonoBehaviour
{
    public GameObject itemSelector;
    public static ItemStorageHandler Instance;
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
        Debug.Log("withdraw");
        //check if exiting this state allows movement
        
        // var pcUsageSelectables =new List<SelectableUI>
        // {
        //     new(pcItemOptions[0], ItemStorageHandler.Instance.ViewItemsToWithdraw, true),
        //     new(pcItemOptions[1], OpenBagToDeposit, true),
        //     new(pcItemOptions[2], ()=>ClosePCOptions(pcItemOptionsUI), true),
        // };
        // InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.ItemStorageNaviagtion,
        //     new[] { InputStateHandler.StateGroup.None},true,pcItemOptionsUI,
        //     InputStateHandler.Directional.Vertical, pcUsageSelectables,ItemStorageHandler.Instance.itemSelector,true, true));
        
    }
    public void OpenBagToDepositItem()
    {
        Bag.Instance.OnItemSelected += DepositItem;
        Bag.Instance.currentBagUsage = Bag.BagUsage.SelectionOnly;
        Game_ui_manager.Instance.ViewBag();
    }
    public void OpenBagToTossItem()
    {
        Bag.Instance.OnItemSelected += TossItem;
        Bag.Instance.currentBagUsage = Bag.BagUsage.SelectionOnly;
        Game_ui_manager.Instance.ViewBag();
    }
    private void DepositItem(Item item)
    {
        Debug.Log("deposited "+item.itemName);
    }
    private void TossItem(Item item)
    {
        Debug.Log("tossed "+item.itemName);
    }
}
