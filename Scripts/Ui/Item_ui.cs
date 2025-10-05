using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Item_ui : MonoBehaviour
{
    public Item item;
    public Text quantity;
    public Text itemName;

    public void LoadItemUI()
    {
        itemName.text = item.itemName;
        quantity.text = "X"+item.quantity;
    }
    public void LoadItemDescription()
    {
        Bag.Instance.currentItemDescription.text = item.itemDescription;
        Bag.Instance.currentItemImage.sprite = item.itemImage;
    }
    public void ResetUI()
    {
        Bag.Instance.currentItemDescription.text = "";
        Bag.Instance.currentItemImage.sprite = null;
    }
}
