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
    public Button Use, Give, Drop;
    public void LoadItemUI()
    {
        itemName.text = item.itemName;
        quantity.text = "X"+item.quantity;
    }
    public void LoadItemDescription()
    {
        Drop.interactable = !Options_manager.Instance.playerInBattle;
        if (Options_manager.Instance.playerInBattle)
        {
            Use.interactable = item.canBeUsedInBattle;
            Give.interactable = false;
        }
        else
        {
            Use.interactable = item.canBeUsedInOverworld;
            if (item.isHeldItem)
                Use.interactable = false;
            Give.interactable = item.canBeHeld;
        }
        itemDescription.text = item.itemDescription;
        itemImg.sprite = item.itemImage;
    }
    public void ResetUI()
    {
        itemDescription.text = "";
        itemImg.sprite = null;
    }
}
