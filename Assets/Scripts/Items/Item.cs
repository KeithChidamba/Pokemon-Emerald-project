using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Item", menuName = "PokeMart/Item")]
public class Item : InfoModuleHolder
{
    [Header("Information")]
    [FormerlySerializedAs("Item_name")] public string itemName = "";
    public ItemType itemType;
    [FormerlySerializedAs("Item_desc")] public string itemDescription = "";
    [FormerlySerializedAs("Item_img")] public Sprite itemImage;
    [Header("Usage")]
    public int quantity;
    [FormerlySerializedAs("ForPartyUse")] public bool forPartyUse = true;
    [FormerlySerializedAs("CanBeUsedInOverworld")] public bool canBeUsedInOverworld = true;
    [FormerlySerializedAs("CanBeUsedInBattle")] public bool canBeUsedInBattle = true;
    public bool isHeldItem;
    [FormerlySerializedAs("CanBeHeld")] public bool canBeHeld;
    
    [Header("Economy")]
    public int buyPrice;
    public bool canBeSold;
    public int sellPrice;
    public ItemPriceCurrency priceCurrency;
}
public enum ItemPriceCurrency
{
    Money,
    None
}