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
                _playerBag.OnItemSold += CheckForItemObjectiveClear; 
                break;
            case MarketObjectiveType.BuyItem: 
                _pokeMartHandler.OnItemBought += CheckForItemObjectiveClear;
                break;
        }
    }

    private void CheckForItemObjectiveClear(Item item)
    {
        if (itemForObjective.itemName == item.itemName)
        {
            _pokeMartHandler.OnItemBought -= CheckForItemObjectiveClear;
            _playerBag.OnItemSold -= CheckForItemObjectiveClear; 
            ClearObjective();
        }
    }
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}
