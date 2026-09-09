using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEndToEndTest : EndToEndTest
{
    private PlayerBagHandler _playerBag;
    
    protected void LoadItems(List<TestItem>testItems)
    {
        _playerBag = serviceContainer.Resolve<PlayerBagHandler>();
        _playerBag.allItems.Clear();
        foreach (var itemData in testItems)
        {
            var newItem = InstanceFactory.CreateItem(itemData.itemAsset);
            newItem.quantity = itemData.quantity;
            _playerBag.AddItem(newItem);
        }   
    }
}
