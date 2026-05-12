using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Item", menuName = "PokeMart/Item")]
public class Item : ScriptableObject
{
    [FormerlySerializedAs("Item_ID")] public string itemID = "";
    [FormerlySerializedAs("Item_name")] public string itemName = "";
    public ItemType itemType;
    [FormerlySerializedAs("Item_effect")] public string itemEffect = "";
    [FormerlySerializedAs("Item_desc")] public string itemDescription = "";
    public int price = 0;
    public int quantity = 0;
    [FormerlySerializedAs("Item_img")] public Sprite itemImage;
    [FormerlySerializedAs("ForPartyUse")] public bool forPartyUse = true;
    [FormerlySerializedAs("CanBeUsedInOverworld")] public bool canBeUsedInOverworld = true;
    [FormerlySerializedAs("CanBeUsedInBattle")] public bool canBeUsedInBattle = true;
    public bool isHeldItem = false;
    [FormerlySerializedAs("CanBeHeld")] public bool canBeHeld = false;
    [FormerlySerializedAs("CanBeSold")] public bool canBeSold = true;
    public bool isMultiModular;
    public bool hasModules = true;
    [FormerlySerializedAs("additionalItemInfo")] public AdditionalInfoModule additionalInfoModule;
    public List<AdditionalInfoModule> additionalInfoModules;
    public List<string> infoModuleAssetNames; //only gets modified and used in code
    public string imageDirectory;//only gets modified and used in code
    public T GetModule<T>() where T : AdditionalInfoModule
    {
        if (isMultiModular)
        {
            return additionalInfoModules.FirstOrDefault(m => m is T) as T;
        }
        return additionalInfoModule as T;
    }

    public void SaveModuleNames()
    {
        if(hasModules)
        {
            infoModuleAssetNames.Clear();
            if (additionalInfoModules.Count == 0 && !isMultiModular)
            {
                //just in-case
                additionalInfoModules.Add(additionalInfoModule);
            }
            foreach (var module in additionalInfoModules)
            {
                infoModuleAssetNames.Add(module.name);
            }
        }
    }
    public void DetermineImageDirectory()
    {
        if (additionalInfoModules.Any(m => m is TM))
        {
            var tm =  GetModule<TM>();
            imageDirectory = tm.move.type.GetTypeName.ToLower() + " tm";
            return;
        }
        if (additionalInfoModules.Any(m => m is HM))
        {
            var hm = GetModule<HM>();
            imageDirectory = hm.move.type.GetTypeName.ToLower() + " tm";
            return;
        }
        imageDirectory = itemName;
    }
    public void LoadData()
    {
        if (hasModules)
        {
            additionalInfoModules.Clear();
            foreach (var assetName in infoModuleAssetNames)
            {
                var additionalInfo = Resources.Load<AdditionalInfoModule>(SaveDataHandler.GetDirectory(AssetDirectory.AdditionalInfo)+assetName);
                additionalInfoModules.Add(additionalInfo);
            }
            additionalInfoModule = additionalInfoModules.First();
        }
        itemImage = Testing.GetValidImage(SaveDataHandler.GetDirectory(AssetDirectory.UI),imageDirectory);
    }
}
