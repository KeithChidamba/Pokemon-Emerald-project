using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEndToEndTest : EndToEndTest
{
    private PlayerBagHandler _playerBag;
    
    protected void LoadItems(List<Item>itemAssets)
    {
        _playerBag = serviceContainer.Resolve<PlayerBagHandler>();
        _playerBag.allItems.Clear();
        foreach (var asset in itemAssets)
        {
            _playerBag.AddItem(asset);
        }   
    }
}
