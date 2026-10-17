using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemSaveData
{
    public List<SavedItem> saveItems = new();
    
    public void SaveItemList(List<Item> items,string nullMessage)
    {
        saveItems.Clear();
        foreach (var item in items)
        {
            if (item is null) throw new Exception(nullMessage);
            saveItems.Add(new SavedItem
            {
                itemAssetName = item.itemName,
                quantity = item.quantity,
            });
        }
    }

    public List<Item> GetSaveItems()
    {
        List<Item> itemList = new();
        foreach (var save in saveItems)
        {
            var itemAsset = Resources.Load<Item>(DirectoryHandler.GetDirectory(AssetDirectory.Items)
                                                     + save.itemAssetName);
            var newItem = InstanceFactory.CreateItem(itemAsset);
            newItem.quantity = save.quantity;
            itemList.Add(newItem);
        }
        return itemList; 
    }
}
[Serializable]
public struct SavedItem
{
    public string itemAssetName;
    public int quantity;
}