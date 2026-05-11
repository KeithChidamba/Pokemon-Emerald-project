using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBagInputService
{
    private Bag _playerBagHandler;
    private ItemStorageHandler _itemStorageHandler;
    private InputStateHandler _inputStateHandler;
    
    public PlayerBagInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _playerBagHandler = container.Resolve<Bag>();
        _itemStorageHandler = container.Resolve<ItemStorageHandler>();
    }
    public void DetermineOperation()
    {
        Action stateMethod = _inputStateHandler.currentState.stateName switch
        {
            InputStateName.PlayerBagNavigation => PlayerBagNavigation,
            InputStateName.PlayerBagItemSell => ItemToSellInputs,
            _ => null
        };
        stateMethod?.Invoke();
    }
    public void PlayerBagNavigationRestrictions()
    {
        ref InputState currentState = ref _inputStateHandler.currentState;
        currentState.currentSelectionIndex = 0;
        if(_playerBagHandler.numItems==0)
        {
            currentState.displayingSelector = false;
            _playerBagHandler.itemSelector.SetActive(false);
            return;
        }
        if(_playerBagHandler.numItems==_playerBagHandler.numItemsForView)
        {
            //prevent selecting null item selectables
            currentState.maxSelectionIndex = _playerBagHandler.numItems-1;
            _inputStateHandler.UpdateSelectorUi();
        }
        currentState.displayingSelector = true;
        _playerBagHandler.itemSelector.SetActive(true);
        switch (_playerBagHandler.currentBagUsage)
        {
            case BagUsage.SellingView:
                currentState.selectableUis.ForEach(s=>s.eventForUi = CreateSellingItemState);
                break;
            case BagUsage.NormalView:
                currentState.selectableUis.ForEach(s=>s.eventForUi = _playerBagHandler.UseItem);
                break;
            case BagUsage.SelectionOnly:
                currentState.selectableUis.ForEach(s=>s.eventForUi = _playerBagHandler.SelectItemForEvent);
                break;
        }
    }

    private void PlayerBagNavigation()
    {
        if(_itemStorageHandler.currentUsage != ItemUsage.Deposit)
        {
            _inputStateHandler.OnInputLeft += _playerBagHandler.ChangeCategoryLeft;
            _inputStateHandler.OnInputRight += _playerBagHandler.ChangeCategoryRight;
        }
        _inputStateHandler.OnInputUp += _playerBagHandler.NavigateUp;
        _inputStateHandler.OnInputDown += _playerBagHandler.NavigateDown;
        PlayerBagNavigationRestrictions();
    }

    private void CreateSellingItemState()
    {
        var itemSellSelectables = new List<SelectableUI>{new(_playerBagHandler.sellingItemUI,_playerBagHandler.SellToMarket,true)};
        _inputStateHandler.ChangeInputState(new (InputStateName.PlayerBagItemSell,
            InputStateGroup.Bag, stateDirection:InputDirection.Vertical, selectableUis:itemSellSelectables
            ,selecting:false,onExit:_playerBagHandler.ResetItemSellingUi,onClose:_playerBagHandler.ResetItemSellingUi));
        _playerBagHandler.ChangeQuantity(0);//initial set for visuals
    }

    private void ItemToSellInputs()
    {
        _inputStateHandler.OnInputUp += ()=>_playerBagHandler.ChangeQuantity(1);
        _inputStateHandler.OnInputDown += ()=>_playerBagHandler.ChangeQuantity(-1);
    }
}