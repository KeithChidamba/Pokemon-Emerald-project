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
    public GameObject equippedMarker;
    
    private Bag _playerBagHandler;
    public void Inject(ServiceContainer container)
    {
        _playerBagHandler = container.Resolve<Bag>();
    }
    
    public void LoadItemUI()
    {
        itemName.text = item.itemName;
        quantity.gameObject.SetActive(!_playerBagHandler.ViewingKeyItems());
        
        if (_playerBagHandler.CanShowEquippedMarker(item.itemName))
        {
            equippedMarker.SetActive(true);
            return;
        }
        equippedMarker.SetActive(false);
        quantity.text = "X"+item.quantity;
    }
    
    public void LoadItemDescription()
    {
        _playerBagHandler.currentItemDescription.text = item.itemDescription;
        _playerBagHandler.currentItemImage.sprite = item.itemImage;

        Utility.ResizeImageToSprite(ref _playerBagHandler.currentItemImage, _playerBagHandler.itemImageTargetSize);
    }

    
    public void ResetUI()
    {
        _playerBagHandler.currentItemDescription.text = "";
        _playerBagHandler.currentItemImage.sprite = null;
    }
}
