using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Store_Item_ui : MonoBehaviour
{
    public Item item;
    [FormerlySerializedAs("Price")] public Text price;
    [FormerlySerializedAs("item_name")] public Text itemName;
    [FormerlySerializedAs("item_description")] public Text itemDescription;
    [FormerlySerializedAs("item_img")] public Image itemImage;
    public void LoadItemUI()
    {
        itemName.text = item.itemName;
        price.text = item.price.ToString();
    }
    public void LoadItemDescription()
    {
        itemDescription.text = item.itemDescription;
        itemImage.sprite = item.itemImage;
    }
    public void ResetUI()
    {
        itemDescription.text = "";
        itemImage.sprite = null;
    }
}
