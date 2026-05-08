using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "market ui obj", menuName = "Objectives/market ui objective")]
public class MarketUiObjective : ItemUiObjective
{
    private enum MarketObjectiveType
    {
        SellItem,BuyItem
    }
    [SerializeField] private MarketObjectiveType marketObjectiveType;

    protected override void LogicForObjectiveLoad()
    {
        switch(marketObjectiveType)
        {
            case MarketObjectiveType.SellItem: SetupItemSellObjective(); break;
            case MarketObjectiveType.BuyItem: SetupItemBuyObjective(); break;
        }
    }

    private void SetupItemSellObjective()
    {
        var playerBag = serviceContainer.Resolve<Bag>(); 
        playerBag.OnItemSold += CheckForItemObjectiveClear;
    }

    private void SetupItemBuyObjective()
    {
        var pokeMartHandler = serviceContainer.Resolve<Poke_Mart>(); 
       pokeMartHandler.OnItemBought += CheckForItemObjectiveClear;
    }
  
}
