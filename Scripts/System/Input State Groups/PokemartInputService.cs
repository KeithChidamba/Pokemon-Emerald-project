using System;
using System.Collections.Generic;

public class PokemartInputService: IInputGroup
{
    private Poke_Mart _pokeMartHandler;
    private InputStateHandler _inputStateHandler;
    
    public PokemartInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _pokeMartHandler = container.Resolve<Poke_Mart>();
    }
    public void DetermineOperation()
    {
        Action stateMethod = _inputStateHandler.currentState.stateName switch
        {
            InputStateName.MartItemNavigation => PokeMartNavigation,
            InputStateName.MartItemPurchase => ItemToBuyInputs,
            _ => null
        };
        stateMethod?.Invoke();
    }
    private void PokeMartNavigation()
    {
        _inputStateHandler.OnInputUp += _pokeMartHandler.NavigateUp;
        _inputStateHandler.OnInputDown += _pokeMartHandler.NavigateDown;
        if(_pokeMartHandler.numItemsForView==_pokeMartHandler.numItems)
        {//prevent selecting null item selectables
            _inputStateHandler.currentState.maxSelectableIndex = _pokeMartHandler.numItems-1;
        }
        _inputStateHandler.currentState.selectableUis.ForEach(s=>s.eventForUi = SelectItemToBuy);
    }

    private void SelectItemToBuy()
    { 
        _pokeMartHandler.quantityUI.SetActive(true);
        var itemQuantitySelectables = new List<SelectableUI>
        {
            new(_pokeMartHandler.quantityUI,_pokeMartHandler.BuyItem,true)
        };
        _inputStateHandler.ChangeInputState(new (InputStateName.MartItemPurchase,InputStateGroup.PokeMart
            , stateDirection:InputDirection.Vertical, selectableUis:itemQuantitySelectables
            ,selector:_pokeMartHandler.quantitySelector,display: true
            ,onExit: ()=>_pokeMartHandler.selectedItemQuantity=1));
    }

    private void ItemToBuyInputs()
    {
        _inputStateHandler.OnInputUp += ()=>_pokeMartHandler.ChangeItemQuantity(1);
        _inputStateHandler.OnInputDown += ()=>_pokeMartHandler.ChangeItemQuantity(-1);
    }
}
