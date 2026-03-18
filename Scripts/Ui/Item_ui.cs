using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Item_ui : MonoBehaviour,IInjectable
{
    public Item item;
    public Text quantity;
    public Text itemName;

    private Bag _playerBagHandler;
    public void Inject(Container container)
    {
        _playerBagHandler = container.Resolve<Bag>();
    }
    
    public void LoadItemUI()
    {
        itemName.text = item.itemName;
        quantity.text = "X"+item.quantity;
    }
    public void LoadItemDescription()
    {
        _playerBagHandler.currentItemDescription.text = item.itemDescription;
        _playerBagHandler.currentItemImage.sprite = item.itemImage;
    }
    public void ResetUI()
    {
        _playerBagHandler.currentItemDescription.text = "";
        _playerBagHandler.currentItemImage.sprite = null;
    }
}
