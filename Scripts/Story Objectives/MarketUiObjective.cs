using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "market ui obj", menuName = "market ui objective")]
public class MarketUiObjective : ItemUiObjective
{
    private enum MarketObjectiveType
    {
        SellItem,BuyItem
    }
    [SerializeField] private MarketObjectiveType marketObjectiveType;
    protected override void OnObjectiveLoaded()
    {
        switch(marketObjectiveType)
        {
            case MarketObjectiveType.SellItem: SetupItemSellObjective(); break;
            case MarketObjectiveType.BuyItem: SetupItemBuyObjective(); break;
        }
    }
    private void SetupItemSellObjective()
    {
        Bag.Instance.OnItemSold += CheckForItemObjectiveClear;
    }

    private void SetupItemBuyObjective()
    {
        Poke_Mart.Instance.OnItemBought += CheckForItemObjectiveClear;
    }
  
}
