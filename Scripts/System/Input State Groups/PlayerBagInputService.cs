using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBagInputService
{
    private Bag _playerBagHandler;
    private ItemStorageHandler _itemStorageHandler;
    private InputState _currentState;
    private InputStateHandler _inputStateHandler;
    
    public PlayerBagInputService(Container container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _playerBagHandler = container.Resolve<Bag>();
        _itemStorageHandler = container.Resolve<ItemStorageHandler>();
        _currentState = _inputStateHandler.currentState;

    }
    public void DetermineOperation()
    {
        Action stateMethod = _currentState.stateName switch
        {
            InputStateName.PlayerBagNavigation => PlayerBagNavigation,
            InputStateName.PlayerBagItemSell => ItemToSellInputs,
            _ => null
        };
        stateMethod?.Invoke();
    }
    public void PlayerBagNavigationRestrictions()
    {
        _currentState.currentSelectionIndex = 0;
        if(_playerBagHandler.numItems==_playerBagHandler.numItemsForView)
        {
            //prevent selecting null item selectables
            _currentState.maxSelectionIndex = _playerBagHandler.numItems-1;
            _inputStateHandler.UpdateSelectorUi();
        }
        _currentState.displayingSelector = _playerBagHandler.numItems > 0;
        _playerBagHandler.itemSelector.SetActive(_playerBagHandler.numItems > 0);
        switch (_playerBagHandler.currentBagUsage)
        {
            case BagUsage.SellingView:
                _currentState.selectableUis.ForEach(s=>s.eventForUi = CreateSellingItemState);
                break;
            case BagUsage.NormalView:
                _currentState.selectableUis.ForEach(s=>s.eventForUi = _playerBagHandler.UseItem);
                break;
            case BagUsage.SelectionOnly:
                _currentState.selectableUis.ForEach(s=>s.eventForUi = _playerBagHandler.SelectItemForEvent);
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