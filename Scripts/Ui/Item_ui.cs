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
    public Button Use, Give, Drop;
    public void Load_item()
    {
        item_name.text = item.itemName;
        quantity.text = "X"+item.quantity.ToString();
    }
    public void Load_item_info()
    {
        if (Options_manager.instance.playerInBattle)
        {
            Drop.interactable = false;
            Give.interactable = false;
            Use.interactable = item.canBeUsedInBattle;
        }
        else
        {
            Drop.interactable = true;
            Use.interactable = item.canBeUsedInOverworld;
            if (item.isHeldItem)
                Use.interactable = false;
            Give.interactable = item.canBeHeld;
        }
        item_description.text = item.itemDescription;
        item_img.sprite=item.itemImage;
    }
    public void Clear_ui()
    {
        item_description.text = "";
        item_img.sprite = null;
    }
}
