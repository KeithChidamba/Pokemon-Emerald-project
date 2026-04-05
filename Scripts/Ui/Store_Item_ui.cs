using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Store_Item_ui : MonoBehaviour,IInjectable
{
    public Item item;
    public Text price;
    public Text itemName;
    public Image itemImage;
    private Poke_Mart _pokeMartHandler;

    public void Inject(ServiceContainer container)
    {
        _pokeMartHandler = container.Resolve<Poke_Mart>();
    }
    public void LoadItemUI()
    {
        itemName.text = item.itemName;
        price.text = item.price.ToString();
    }
    public void LoadItemDescription()
    {
        _pokeMartHandler.itemDescription.text = item.itemDescription;
        itemImage.sprite = item.itemImage;
    }
    public void ClearUI()
    {
        item = null;
        _pokeMartHandler.itemDescription.text = "";
        itemImage.sprite = null;
        gameObject.SetActive(false);
    }
}
