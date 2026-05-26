using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemUsage{Withdraw,Deposit,Toss,None}
public class ItemStorageHandler : MonoBehaviour,IInjectable
{
    public ItemUsage currentUsage;
    
    private Game_ui_manager _gameUIHandler;
    private Dialogue_handler _dialogueHandler;
    private Bag _playerBagHandler;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _playerBagHandler = container.Resolve<Bag>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        
    }
    public void ViewItemsToWithdraw()
    {
        if (_playerBagHandler.storageItems.Count==0)
        {
            _dialogueHandler.DisplayDetails("You have no items to withdraw");
            return;
        }
        currentUsage = ItemUsage.Withdraw;
        _playerBagHandler.OnItemSelected += _playerBagHandler.WithDrawFromStorage;
        _playerBagHandler.currentBagUsage = BagUsage.SelectionOnly;
        _playerBagHandler.storageView = true;
        _gameUIHandler.ValidateBagView();
    }
    public void OpenBagToDepositItem()
    {
        currentUsage = ItemUsage.Deposit;
        _playerBagHandler.OnItemSelected += DepositItem;
        _playerBagHandler.currentBagUsage = BagUsage.SelectionOnly;
        _gameUIHandler.ValidateBagView();
    }
    public void OpenBagToTossItem()
    {
        currentUsage = ItemUsage.Toss;
        _playerBagHandler.OnItemSelected += TossItem;
        _playerBagHandler.currentBagUsage = BagUsage.SelectionOnly;
        _gameUIHandler.ValidateBagView();
    }
    private void DepositItem(Item item)
    {
        _dialogueHandler.DisplayDetails("Sent "+item.itemName+" to storage");
        _playerBagHandler.DepositToStorage(item);
    }
    private void TossItem(Item item)
    {
        _dialogueHandler.DisplayDetails("Threw "+item.itemName+(item.quantity==1?"":"'s ")+" away");
        _playerBagHandler.RemoveItem(item);
    }
}

