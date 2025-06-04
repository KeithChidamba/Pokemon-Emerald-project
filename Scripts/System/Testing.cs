using System;
using System.IO;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;

public class Testing : MonoBehaviour
{
    string[] _types = { "Normal", "Fire", "Water", "Electric", "Grass", "Ice", "Fighting", "Poison",
        "Ground", "Flying", "Psychic", "Bug", "Rock", "Ghost", "Dragon", "Dark", "Steel"
    };

    void LowerCaseFiles(string path)
    {
        // Rename files
        foreach (var filePath in Directory.GetFiles(path))
        {
            string fileName = Path.GetFileName(filePath);
            string lowerFileName = fileName.ToLower();

            if (fileName != lowerFileName)
            {
                string newPath = Path.Combine(path, lowerFileName);
                if (!File.Exists(newPath))  // Prevent overwrite
                {
                    File.Move(filePath, newPath);
                }
            }
        }
    }
    public static Sprite CheckImage(string resourcePath, string itemName)
    {
        Debug.Log("Testing image load for: " + itemName);
        var fullPath = resourcePath + itemName.ToLower();

        var itemSprite = Resources.Load<Sprite>(fullPath);

        if (itemSprite == null)
        {
            Debug.LogWarning("Bag item image not found for: " + itemName);
            return null;
        }

        return itemSprite;
    }
    private void CheckTypes()
    {
        foreach (var type in _types)
        {
            var currentType = Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/"+type);
            CheckType(currentType,currentType.immunities,"immune");
            CheckType(currentType,currentType.weaknesses,"weakness");
            CheckType(currentType,currentType.resistances,"resist");
        }
    }

    void CheckType(Type typeToCheck,string[] listOfTypes,string listName)
    {
        foreach (var type in listOfTypes)
        {
            if (type == "None") return;
            
            if(!_types.Contains(type))
                Debug.Log(type+ " invalid on asset: "+typeToCheck.typeName+"'s "+listName);
            else
                Debug.Log("valid");
        }
    }
}
