using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;

public class AssetHealthChecks
{
    public static Sprite GetValidImage(string resourcePath, string itemName)
    {
        var fullPath = resourcePath + itemName.ToLower();

        var itemSprite = Resources.Load<Sprite>(fullPath);

        if (itemSprite == null)
        {
            Debug.LogWarning("image not found for: " + fullPath+itemName);
            return null;
        }

        return itemSprite;
    }
}
