using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StoreItemUi : MonoBehaviour,IInjectable
{
    public Item item;
    public Text price;
    public Text itemName;
    public Image itemImage;
    private PokeMartHandler _pokeMartHandler;
    
    public void Inject(ServiceContainer container)
    {
        _pokeMartHandler = container.Resolve<PokeMartHandler>();
    }
    
    public void OnInject()
    {
        
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
        Utility.ResizeImageToSprite(ref itemImage, _pokeMartHandler.itemImageTargetSize);
    }
    public void ClearUI()
    {
        item = null;
        _pokeMartHandler.itemDescription.text = "";
        itemImage.sprite = null;
        gameObject.SetActive(false);
    }
}
