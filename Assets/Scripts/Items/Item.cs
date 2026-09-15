using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Item", menuName = "PokeMart/Item")]
public class Item : ScriptableObject
{
    [Header("Information")]
    [FormerlySerializedAs("Item_ID")] public string itemID = "";
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
    
    [Header("Data")]
    public List<AdditionalInfoModule> additionalInfoModules = new();
    [SerializeReference]public List<DynamicAdditionalInfo> dynamicInfoModules = new();
    
    public string imageDirectory;//only gets modified and used in code

    public T GetModule<T>() where T : AdditionalInfoModule
    {
        return additionalInfoModules.FirstOrDefault(m => m is T) as T;
    }
    public T GetDynamicModule<T>() where T : DynamicAdditionalInfo
    {
        return dynamicInfoModules.FirstOrDefault(m => m is T) as T;
    }
    public string DetermineImageDirectory()
    {
        if (dynamicInfoModules.Any(m => m is MoveLearningMachineInfo))
        {
            var machine = GetDynamicModule<MoveLearningMachineInfo>();
            //nameSub = tm or hm
            string nameSub = itemName.Substring(0, 2);
            return machine.move.type.GetTypeName.ToLower() + $" {nameSub.ToLower()}";
        }
        return itemName;
    }
    public void SetImageDirectory()
    {
        imageDirectory = DetermineImageDirectory();
    }
    public void LoadData()
    {
        var sourceAsset = Resources.Load<Item>(DirectoryHandler.GetDirectory(AssetDirectory.Items)+ itemName);
        if(sourceAsset.additionalInfoModules.Count>0){
            additionalInfoModules.Clear();
            additionalInfoModules.AddRange(sourceAsset.additionalInfoModules);
        }
        if(sourceAsset.dynamicInfoModules.Count>0){
            dynamicInfoModules.Clear();
            dynamicInfoModules.AddRange(sourceAsset.dynamicInfoModules);
        }
        
        itemImage = AssetHealthChecks.GetValidImage(DirectoryHandler.GetDirectory(AssetDirectory.ItemUI),imageDirectory);
    }
}
public enum ItemPriceCurrency
{
    Money,
    None
}