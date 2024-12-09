using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Item_ui : MonoBehaviour
{
    public Item item;
    public Text quantity;
    public Text item_name;
    public Text item_description;
    public Image item_img;
    
    public void Load_item()
    {
        item_name.text = item.Item_name;
        quantity.text = "X"+item.quantity.ToString();
    }
    public void Load_item_info()
    {
        item_description.text = item.Item_desc;
        item_img.sprite=item.Item_img;
    }
    public void Clear_ui()
    {
        item_description.text = "";
        item_img.sprite = null;
    }
}
