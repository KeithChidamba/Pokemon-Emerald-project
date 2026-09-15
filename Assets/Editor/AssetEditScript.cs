using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;

public class AssetEditScript : EditorWindow
{
    // Folder containing Item ScriptableObjects
    [MenuItem("Tools/Fix Move typeless")]
    public static void FixMoveTypelessEffect()
    {
        HashSet<MoveName> unaffectedMoves = new()
        {
            MoveName.Agility,
            MoveName.Attract,
            MoveName.BellyDrum,
            MoveName.Bide,
            MoveName.BulkUp,
            MoveName.ConfuseRay,
            MoveName.Detect,
            MoveName.DoubleTeam,
            MoveName.FocusEnergy,
            MoveName.Foresight,
            MoveName.Growl,
            MoveName.Harden,
            MoveName.Haze,
            MoveName.Leer,
            MoveName.LightScreen,
            MoveName.MeanLook,
            MoveName.MirrorMove,
            MoveName.MoonLight,
            MoveName.MorningSun,
            MoveName.MudSport,
            MoveName.OdorSleuth,
            MoveName.Protect,
            MoveName.RainDance,
            MoveName.Reflect,
            MoveName.Rest,
            MoveName.SandStorm,
            MoveName.Screech,
            MoveName.Supersonic,
            MoveName.TailWhip,
            MoveName.Whirlwind
        };

        Debug.Log($"Typeless-effect moves array length: {unaffectedMoves.Count}");

        string folderPath = "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Moves/";
        string[] guids = AssetDatabase.FindAssets("t:Move", new[] { folderPath });

        int counter = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Move move = AssetDatabase.LoadAssetAtPath<Move>(assetPath);

            if (move == null)
            {
                Debug.LogError($"[ERROR] Move asset not found at: {assetPath}");
                continue;
            }

            bool hasTypelessEffect = unaffectedMoves.Contains(NameDB.ParseMoveName(move.moveName));

            move.hasTypelessEffect = hasTypelessEffect;

            if (hasTypelessEffect)
            {
                counter++;
                Debug.Log($"[TYPELESS EFFECT] {move.moveName}");
            }

            EditorUtility.SetDirty(move);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Total moves with typeless effects: {counter}");
        Debug.Log("Move typeless-effect updates complete.");
    }

    [MenuItem("Tools/Assign Item Sprites")]
    public static void AssignSprites()
    {
        // Fetch all Item ScriptableObjects
        var ITEM_FOLDER = "Assets/Resources/" + DirectoryHandler.GetDirectory(AssetDirectory.Items);

        // Folder containing Sprites
        var SPRITE_FOLDER = "Assets/Resources/" + DirectoryHandler.GetDirectory(AssetDirectory.ItemUI);

        string[] itemGuids = AssetDatabase.FindAssets("t:Item", new[] { ITEM_FOLDER });

        int assignedCount = 0;

        foreach (string guid in itemGuids)
        {
            string itemPath = AssetDatabase.GUIDToAssetPath(guid);

            Item item = AssetDatabase.LoadAssetAtPath<Item>(itemPath);

            if (item == null)
            {
                Debug.LogError($"null item: {itemPath}");
                continue;
            }

            var itemImageName = item.DetermineImageDirectory();
            // Debug.Log($"image name: {itemImageName}");
            // Assumes sprite name matches item name
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { SPRITE_FOLDER });

            Sprite matchedSprite = null;

            foreach (string spriteGuid in spriteGuids)
            {
                string spritePath = AssetDatabase.GUIDToAssetPath(spriteGuid);

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                if (sprite == null)
                {
                    Debug.LogError($"null sprite: {spritePath}");
                    continue;
                }

                // Exact name match only
                if (sprite.name == itemImageName.ToLower())
                {
                    matchedSprite = sprite;
                    break;
                }
            }

            if (matchedSprite == null)
            {
                Debug.LogError($"Exact sprite match not found for Item: {item.name}");
                continue;
            }

            // Assign sprite
            item.itemImage = matchedSprite;

            EditorUtility.SetDirty(item);

            assignedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Finished assigning sprites. Assigned: {assignedCount}");
    }

    [MenuItem("Tools/Check Items to make")]
    public static void CheckItemToMake()
    {
        // Fetch all Item ScriptableObjects
        var ITEM_FOLDER = "Assets/Resources/" + DirectoryHandler.GetDirectory(AssetDirectory.Items);

        // Folder containing Sprites
        var SPRITE_FOLDER = "Assets/Resources/" + DirectoryHandler.GetDirectory(AssetDirectory.ItemUI);

        string[] itemGuids = AssetDatabase.FindAssets("t:Item", new[] { ITEM_FOLDER });

        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { SPRITE_FOLDER });

        foreach (string spriteGuid in spriteGuids)
        {
            string spritePath = AssetDatabase.GUIDToAssetPath(spriteGuid);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            if (sprite == null)
            {
                Debug.LogError($"null sprite: {spritePath}");
                continue;
            }

            if (sprite.name.Contains("tm") || sprite.name.Contains("hm")) continue;

            var itemFound = "";
            foreach (string guid in itemGuids)
            {
                string itemPath = AssetDatabase.GUIDToAssetPath(guid);

                Item item = AssetDatabase.LoadAssetAtPath<Item>(itemPath);

                if (item == null)
                {
                    Debug.LogError($"null item: {itemPath}");
                    continue;
                }

                var itemImageName = item.DetermineImageDirectory();
                // Exact name match only
                if (sprite.name == itemImageName.ToLower())
                {
                    itemFound = itemImageName;
                    break;
                }
            }

            if (itemFound == "")
            {

                Debug.Log($"sprite{sprite.name} doesnt have item");
            }
        }

        Debug.Log($"Finished assigning sprites. Checking");
    }
    private static readonly Dictionary<string, int> BuyPrices = new()
    {
        // Poké Mart
        ["Antidote"] = 100,
        ["Awakening"] = 250,
        ["Burn Heal"] = 250,
        ["Escape Rope"] = 550,
        ["Full Heal"] = 600,
        ["Full Restore"] = 3000,
        ["Great Ball"] = 600,
        ["Guard Spec"] = 700,
        ["Hyper Potion"] = 1200,
        ["Ice Heal"] = 250,
        ["Max Potion"] = 2500,
        ["Max Repel"] = 700,
        
        ["Paralyze Heal"] = 200,
        ["Pokeball"] = 200,
        ["Potion"] = 300,
        ["Repel"] = 350,
        ["Revive"] = 1500,
        ["Max Revive"] = 2000,
        ["Super Potion"] = 700,
        ["Super Repel"] = 500,

        // Department Store
        ["Thunder Stone"] = 1050,
        ["Calcium"] = 9800,
        ["Carbos"] = 9800,
        ["HP Up"] = 9800,
        ["Iron"] = 9800,
        ["Protein"] = 9800,
        ["Zinc"] = 9800,
        ["PP Up"] = 2500,
        ["PP Max"] = 5000,
        
        ["X Accuracy"] = 950,
        ["X Attack"] = 500,
        ["X Defense"] = 550,
        ["X Special Attack"] = 350,
        ["X Speed"] = 350,
        ["Dire Hit"] = 650,
        
        // Herb Shop
        ["Energy Powder"] = 500,
        ["Energy Root"] = 800,
        ["Heal Powder"] = 450,
        ["Revival Herb"] = 2800,

        // TMs
        ["TM06"] = 3000,
        ["TM08"] = 3000,
        ["TM09"] = 2000,

        ["TM13"] = 4000,
        ["TM15"] = 7500,

        ["TM18"] = 2500,
        ["TM19"] = 3000,

        ["TM24"] = 4000,
        ["TM25"] = 5500,
        ["TM26"] = 5500,

        ["TM28"] = 2500,
        ["TM31"] = 3000,
        ["TM35"] = 4000,
        ["TM37"] = 2500,
        ["TM40"] = 2500,
        ["Ether"] = 600,
        ["Max Ether"] = 1000,
        ["Elixir"] = 1500,
        ["Max Elixir"] = 2250
    };
    
    private static readonly HashSet<string> CannotBeSold = new()
    {
        "Amulet Coin",
        "Aspear berry",
        "Cheri berry",
        "Chesto berry",
        "Choice Band",
        "Exp Share",
        "Grepa berry",
        "HM02",
        "HM03",
        "Hondew berry",
        "Kelpsy berry",
        "Leppa berry",
        "Lum berry",
        "Luxury Ball",
        "Max Revive",
        "Old Rod",
        "Oran berry",
        "Pecha berry",
        "Persim berry",
        "Pomeg berry",
        "PP Max",
        "PP Up",
        "Qualot berry",
        "Rare Candy",
        "Rawst berry",
        "Sitrus berry",
        "Soothe Bell",
        "Super Rod",
        "Tamato berry",
        "Wailmer Pail"
    };

    [MenuItem("Tools/Items/Update Gen 3 Item Economy")]
    public static void UpdateItems()
    {
        var ITEM_FOLDER = "Assets/Resources/" + DirectoryHandler.GetDirectory(AssetDirectory.Items);

        string[] guids = AssetDatabase.FindAssets(
            "t:Item",
            new[] { ITEM_FOLDER });

        int changed = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Item item = AssetDatabase.LoadAssetAtPath<Item>(path);

            if (item == null)
                continue;

            ConfigureItem(item);

            EditorUtility.SetDirty(item);
            changed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Updated {changed} Gen 3 items.");
    }

    private static void ConfigureItem(Item item)
    {
        // Reset economy data
        item.buyPrice = 0;
        item.sellPrice = 0;
        item.canBeSold = false;
        item.priceCurrency = ItemPriceCurrency.None;

        // -------------------------
        // Explicit exceptions
        // -------------------------

        if (CannotBeSold.Contains(item.itemName))
        {
            item.canBeSold = false;
        }

        // -------------------------
        // Berry
        // -------------------------
        if (item.itemType == ItemType.Berry)
        {
            item.canBeSold = false;
            return;
        }
        
        // -------------------------
        // Known money purchases
        // -------------------------

        if (BuyPrices.TryGetValue(item.itemName, out int price))
        {
            item.canBeSold = true;
            item.buyPrice = price;
            item.sellPrice = price / 2;
            item.priceCurrency = ItemPriceCurrency.Money;
        }
        
        // -------------------------
        // Special sell-only items
        // -------------------------

        if (item.itemName == "Nugget")
        {
            item.canBeSold = true;
            item.sellPrice = 5000;
            item.priceCurrency = ItemPriceCurrency.Money;
        }
    }
}