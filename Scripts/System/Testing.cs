using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;

public class Testing : MonoBehaviour
{
    string[] _types = { "Normal", "Fire", "Water", "Electric", "Grass", "Ice", "Fighting", "Poison",
        "Ground", "Flying", "Psychic", "Bug", "Rock", "Ghost", "Dragon", "Dark", "Steel"
    };
    
    public static void TestMoves()
    {
        string[] types = { "Normal","Normal","Normal","Normal","Normal","Normal","Normal","Normal","Normal"
            ,"Bug","Bug","Dark","Dark","Electric","Electric","Electric","Fighting","Fighting",
            "Fire","Fire","Flying","Grass","Grass","Ground","Ground","Ground", "Poison", "Poison",
            "Psychic","Psychic","Water","Water","Water"
        };
        var counter = 0;
        foreach(var move in NameDB._moveNames)
        {
            Debug.Log("Pokemon_project_assets/Pokemon_obj/Moves/"
                      + types[counter].ToLower() + "/" + move.Value.ToLower());
            var movesTest= Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/"
                                       + types[counter].ToLower() + "/" + move.Value.ToLower());
            counter++;
        }
    }
    public static void LowerCaseFiles(string path)
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

    public static void checkitems()
    {
        List<string[]> itemPools = new()
        {
            new[] { "Potion", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" },
            new[] { "Super Potion", "Escape Rope", "Potion", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" },
             new[] { "Hyper Potion", "Super Potion", "Escape Rope", "Potion", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" },
             new[] { "Ether", "Full Heal", "Hyper Potion", "Super Potion", "Escape Rope", "Potion", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" },
             new[] { "Rare Candy", "Full Heal", "Ether", "Hyper Potion", "Super Potion", "Escape Rope", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" },
            new[] { "Rare Candy", "Full Heal", "Ether", "Revive", "Hyper Potion", "Escape Rope", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" },
             new[] { "Rare Candy", "Full Heal", "Ether", "Revive", "Hyper Potion",  "Escape Rope" },
             new[] { "Rare Candy", "Full Heal", "Ether", "Revive", "Hyper Potion",  "PP Up" }
        };
        var dups = new List<string>();
        foreach (var list in itemPools)
        {
            foreach (var item in list)
            {
                if (!dups.Contains(item))
                {
                    dups.Add(item);
                }
                else
                {
                    continue;
                }
                var itemAsset = Resources.Load<Item>("Pokemon_project_assets/Items/Mart_Items/" + item);
                
                if (itemAsset == null)
                {
                    Debug.LogWarning("item not found for: " + item);
                }
            }
        }
    }
    public static Sprite CheckImage(string resourcePath, string itemName)
    {
       // Debug.Log("Testing image load for: " + itemName);
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
