using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Store_Item_ui : MonoBehaviour
{
    public Item item;
    public Text price;
    public Text itemName;
    public Image itemImage;
    public void LoadItemUI()
    {
        itemName.text = item.itemName;
        price.text = item.price.ToString();
    }
    public void LoadItemDescription()
    {
        Poke_Mart.Instance.itemDescription.text = item.itemDescription;
        itemImage.sprite = item.itemImage;
    }
    public void ClearUI()
    {
        item = null;
        Poke_Mart.Instance.itemDescription.text = "";
        itemImage.sprite = null;
        gameObject.SetActive(false);
    }
}
