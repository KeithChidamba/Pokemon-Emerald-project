using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBagInputService : IInputGroup
{
    private PlayerBagHandler _playerBagHandler;
    private ItemStorageHandler _itemStorageHandler;
    private InputStateHandler _inputStateHandler;
    private DialogueHandler _dialogueHandler;
    
    public PlayerBagInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _playerBagHandler = container.Resolve<PlayerBagHandler>();
        _itemStorageHandler = container.Resolve<ItemStorageHandler>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
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
    private void ItemToSellInputs()
    {
        _inputStateHandler.OnInputUp += ()=>_playerBagHandler.ChangeQuantity(1);
        _inputStateHandler.OnInputDown += ()=>_playerBagHandler.ChangeQuantity(-1);
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
        
        ref InputState currentState = ref _inputStateHandler.currentState;
        currentState.currentSelectionIndex = 0;
        if(_playerBagHandler.NumItems==0)
        {
            currentState.displayingSelector = false;
            _playerBagHandler.itemSelector.SetActive(false);
            return;
        }
        if(_playerBagHandler.NumItems==_playerBagHandler.NumItemsForView)
        {
            //prevent selecting null item selectables
            currentState.maxSelectableIndex = _playerBagHandler.NumItems-1;
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
        return;
        void CreateSellingItemState()
        {
            if(_playerBagHandler.GetCurrentItem().priceCurrency == ItemPriceCurrency.Money
               && _playerBagHandler.GetCurrentItem().canBeSold)
            {
                var itemSellSelectables = new List<SelectableUI>
                {
                    new(_playerBagHandler.sellingItemUI, _playerBagHandler.SellToMarket, true)
                };
            
                _inputStateHandler.ChangeInputState(new(InputStateName.PlayerBagItemSell,
                    InputStateGroup.Bag, stateDirection: InputDirection.Vertical, selectableUis: itemSellSelectables
                    , selecting: false, onExit: _playerBagHandler.ResetItemSellingUi,
                    onClose: _playerBagHandler.ResetItemSellingUi));
            
                _playerBagHandler.ChangeQuantity(0); //initial set for visuals
            }else
            {
                _dialogueHandler.DisplayDetails("You cant sell that!");
            }
        }
    }
}