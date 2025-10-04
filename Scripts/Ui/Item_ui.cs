using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Item_ui : MonoBehaviour
{
    public Item item;
    public Text quantity;
    [FormerlySerializedAs("item_name")] public Text itemName;
    [FormerlySerializedAs("item_description")] public Text itemDescription;
    [FormerlySerializedAs("item_img")] public Image itemImg;
    public void LoadItemUI()
    {
        itemName.text = item.itemName;
        quantity.text = "X"+item.quantity;
    }
    public void LoadItemDescription()
    {
        itemDescription.text = item.itemDescription;
        itemImg.sprite = item.itemImage;
    }
    public void ResetUI()
    {
        itemDescription.text = "";
        itemImg.sprite = null;
    }
}
