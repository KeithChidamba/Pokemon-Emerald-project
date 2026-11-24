using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "market ui obj", menuName = "Objectives/market ui objective")]
public class MarketUiObjective : StoryObjective
{
    public Item itemForObjective;
    
    private enum MarketObjectiveType
    {
        SellItem,BuyItem
    }
    [SerializeField] private MarketObjectiveType marketObjectiveType;
    
    private PokeMartHandler _pokeMartHandler; 
    private PlayerBagHandler _playerBag; 
    
    protected override void OnObjectiveLoaded()
    {
        var dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
        _pokeMartHandler = serviceContainer.Resolve<PokeMartHandler>(); 
        _playerBag = serviceContainer.Resolve<PlayerBagHandler>();
        
        switch(marketObjectiveType)
        {
            case MarketObjectiveType.SellItem:
                _playerBag.restrictingSell = true;
                _playerBag.itemRestriction = itemForObjective;
                _playerBag.OnItemSold += ClearStateAfterItemObjective; 
                break;
            case MarketObjectiveType.BuyItem: 
                _pokeMartHandler.restrictingBuy = true;
                _pokeMartHandler.itemRestriction = itemForObjective;
                _pokeMartHandler.OnItemBought += ClearStateAfterItemObjective;
                break;
        }
    }

    private void ClearStateAfterItemObjective(Item item)
    {
        switch(marketObjectiveType)
        {
            case MarketObjectiveType.SellItem:
                _playerBag.restrictingSell = false;
                _playerBag.itemRestriction = null;
                _playerBag.OnItemSold -= ClearStateAfterItemObjective; 
                break;
            case MarketObjectiveType.BuyItem: 
                _pokeMartHandler.restrictingBuy = false;
                _pokeMartHandler.itemRestriction = null;
                _pokeMartHandler.OnItemBought -= ClearStateAfterItemObjective;
                break;
        }
        ClearObjective();
    }
    
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}
