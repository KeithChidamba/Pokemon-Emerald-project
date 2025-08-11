using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class NatureEVFixer : EditorWindow
{
    // [MenuItem("Tools/Add list info modules")]
    // public static void AddInfoModules()
    // {
    //     //add other item dir
    //     string folderPath = "Assets/Resources/Pokemon_project_assets/Items/Berries/";
    //     string[] guids = AssetDatabase.FindAssets("t:Item", new[] { folderPath });
    //     
    //     foreach (string guid in guids)
    //     {
    //         string assetPath = AssetDatabase.GUIDToAssetPath(guid);
    //         Item item = AssetDatabase.LoadAssetAtPath<Item>(assetPath);
    //     
    //         if (item != null)
    //         {
    //             item.additionalInfoModules.Add(item.additionalItemInfo);
    //             EditorUtility.SetDirty(item); // Mark as dirty for saving
    //             Debug.Log($"Updated info module: {item.name}");
    //         }
    //     }
    //     AssetDatabase.SaveAssets();
    //     AssetDatabase.Refresh();
    //     Debug.Log("module updates complete.");
    // }

    // [MenuItem("Tools/Add stat info modules")]
    // public static void CreateStats()
    // {
    //     var statList = new[]
    //     {
    //         PokemonOperations.Stat.Attack,PokemonOperations.Stat.Defense,
    //         PokemonOperations.Stat.SpecialAttack,PokemonOperations.Stat.SpecialDefense,
    //         PokemonOperations.Stat.Speed,PokemonOperations.Stat.Hp,
    //         PokemonOperations.Stat.Crit,PokemonOperations.Stat.Accuracy,PokemonOperations.Stat.Evasion
    //     };
    //     foreach (var stat in statList)
    //     {
    //         SaveScriptableObject<StatInfo>(
    //             Save_manager.AssetDirectory.AdditionalInfo,
    //             stat.ToString(),
    //             statAsset =>
    //             {
    //                 statAsset.statName = stat;
    //                 
    //             }
    //         );
    //     }
    // }
    //
    //
    // [MenuItem("Tools/Add Tm's items")]
    // public static void CreateTms()
    // {
    //     string[] tmDescriptions = new string[]
    //     {
    //         //create the description of the tms you want to add
    //     };
    //
    //     var directory = Save_manager.GetDirectory(Save_manager.AssetDirectory.AdditionalInfo);
    //     var tmList = Resources.LoadAll<TM>(directory.TrimEnd('/'));
    //     int count = 0;
    //
    //     foreach (var tmData in tmList)
    //     {
    //         SaveScriptableObject<Item>(
    //             Save_manager.AssetDirectory.NonMartItems,
    //             tmData.name,
    //             tm =>
    //             {
    //                 tm.itemName = tmData.move.moveName;
    //                 tm.itemDescription = tmDescriptions[count];
    //                 tm.additionalItemInfo = tmData;
    //                 tm.canBeSold = true;
    //                 tm.forPartyUse = true;
    //                 tm.canBeUsedInOverworld = true;
    //                 tm.canBeUsedInBattle = false;
    //             }
    //         );
    //         count++;
    //     }
    // }
    //
    // private static Dictionary<Save_manager.AssetDirectory, string> _directories = new()
    // {
    //     { Save_manager.AssetDirectory.Moves, "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Moves/" },
    //     { Save_manager.AssetDirectory.Status, "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Status/" },
    //     { Save_manager.AssetDirectory.Pokemon, "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Pokemon/" },
    //     { Save_manager.AssetDirectory.PokemonImage, "Assets/Resources/Pokemon_project_assets/pokemon_img/" },
    //     { Save_manager.AssetDirectory.Abilities, "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Abilities/" },
    //     { Save_manager.AssetDirectory.Types, "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Types/" },
    //     { Save_manager.AssetDirectory.Natures, "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Natures/" },
    //     { Save_manager.AssetDirectory.UI, "Assets/Resources/Pokemon_project_assets/Pokemon_obj/UI/" },
    //     { Save_manager.AssetDirectory.NonMartItems, "Assets/Resources/Pokemon_project_assets/Items/NonMartItems/" },
    //     { Save_manager.AssetDirectory.MartItems, "Assets/Resources/Pokemon_project_assets/Items/Mart_Items/" },
    //     { Save_manager.AssetDirectory.Items, "Assets/Resources/Pokemon_project_assets/Items/" },
    //     { Save_manager.AssetDirectory.AdditionalInfo,"Assets/Resources/Pokemon_project_assets/Items/AdditionalInfo/" }
    // };
    //
    // public static void SaveScriptableObject<T>(Save_manager.AssetDirectory directory,
    //     string assetName, System.Action<T> onSetup) where T : ScriptableObject
    // {
    //     if (!_directories.TryGetValue(directory, out string folderPath))
    //     {
    //         Debug.LogError($"Directory mapping for {directory} not found.");
    //         return;
    //     }
    //
    //     // Ensure the folder path exists (recursive creation)
    //     if (!Directory.Exists(folderPath))
    //     {
    //         Debug.Log("dir not found : "+folderPath);
    //         Directory.CreateDirectory(folderPath);
    //         AssetDatabase.Refresh();
    //     }
    //
    //     string assetPath = $"{folderPath}{assetName}.asset";
    //     T existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
    //
    //     if (existingAsset == null)
    //     {
    //         T asset = ScriptableObject.CreateInstance<T>();
    //         onSetup?.Invoke(asset);
    //         AssetDatabase.CreateAsset(asset, assetPath);
    //         EditorUtility.SetDirty(asset);
    //         AssetDatabase.SaveAssets();
    //         AssetDatabase.Refresh();
    //         Debug.Log($"✅ Created new asset at: {assetPath}");
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"⚠ Asset already exists at {assetPath}");
    //     }
    // }
    // [MenuItem("Tools/Fix Pokemon Natures")]
    // public static void FixNatures()
    // {
    //     string folderPath = "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Natures/";
    //
    //     string[] guids = AssetDatabase.FindAssets("t:Nature", new[] { folderPath });
    //
    //     foreach (string guid in guids)
    //     {
    //         string assetPath = AssetDatabase.GUIDToAssetPath(guid);
    //         Nature nature = AssetDatabase.LoadAssetAtPath<Nature>(assetPath);
    //
    //         if (nature != null)
    //         {
    //             ApplyStatChanges(nature);
    //             EditorUtility.SetDirty(nature); // Mark as dirty for saving
    //             Debug.Log($"Updated: {nature.name}");
    //         }
    //     }
    //
    //     AssetDatabase.SaveAssets();
    //     AssetDatabase.Refresh();
    //     Debug.Log("Nature updates complete.");
    // }
//     [MenuItem("Tools/Fix Move Contact")]
//     public static void FixMoveContact()
//     {
//         string[] contactMoves = new string[]
// {
//     // 🐞 Bug-type
//     "Fury Cutter",
//     "Leech Life",
//
//     // 🥋 Fighting-type
//     "Brick Break",
//     "Double Kick",
//     "Sky Uppercut",
//
//     // 🔥 Fire-type
//     "Blaze Kick",
//     "Fire Punch",
//
//     // 🛫 Flying-type
//     "Aerial Ace",
//     "Peck",
//     "Wing Attack",
//
//     // 🌿 Grass-type
//     "Absorb",
//     "Giga Drain",
//     "Leaf Blade",
//     "Mega Drain",
//
//     // 🌍 Ground-type
//     "Dig",
//
//     // 💜 Normal-type
//     "Bide",
//     "Covet",
//     "Endeavor",
//     "False Swipe",
//     "Flail",
//     "Fury Swipes",
//     "Headbutt",
//     "Hyper Beam",
//     "Pound",
//     "Quick Attack",
//     "Scratch",
//     "Slam",
//     "Slash",
//     "Tackle",
//     "Take Down",
//
//     // 💀 Poison-type
//     "Poison Fang",
//
//     // 🌊 Water-type
//     "Muddy Water",
//
//     // 👻 Ghost-type
//     "Astonish",
//
//     // 🌑 Dark-type
//     "Bite",
//     "Crunch"
// };
//        Debug.Log($"Contact moves array length: {contactMoves.Length}");
//
//         string folderPath = "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Moves/";
//         string[] guids = AssetDatabase.FindAssets("t:Move", new[] { folderPath });
//
//         int counter = 0; // counts how many moves were set to contact
//         int invalidNameCount = 0;
//
//         foreach (string guid in guids)
//         {
//             string assetPath = AssetDatabase.GUIDToAssetPath(guid);
//             Move move = AssetDatabase.LoadAssetAtPath<Move>(assetPath);
//
//             if (move != null)
//             {
//                 //correcting names
//                 // var validName = NameDB.GetMoveName(move.name);
//                 // move.moveName = validName;
//                 // Debug.LogWarning("valid name: "+validName);
//                 
//                 //correcting asset names
//                 // var validName = NameDB.GetMoveName(move.moveName);
//                 // if (validName)
//                 // {
//                 //     Debug.Log($"[VALID] {move.name} resolved as \"{move.moveName}\"");
//                 // }
//                 // else
//                 // {
//                 //     Debug.LogWarning($"[INVALID NAME] {move.name} does not match NameDB.");
//                 //     invalidNameCount++;
//                 // }
//
//                 // //Check if this move is in the contact moves list
//                 if (contactMoves.Contains(move.moveName))
//                 {
//                     move.isContact = true;
//                     counter++;
//                     Debug.LogWarning($"[CONTACT] {move.moveName} marked as contact.");
//                 }
//                 else
//                 {
//                     move.isContact = false;
//                     Debug.Log($"[NON-CONTACT] {move.moveName} marked as non-contact.");
//                 }
//
//                 EditorUtility.SetDirty(move); // Mark asset as dirty for saving
//             }
//             else
//             {
//                 Debug.LogError($"[ERROR] Move asset not found at: {assetPath}");
//             }
//         }
//
//         Debug.Log($"Total contact moves updated: {counter}");
//         Debug.Log($"Invalid name count: {invalidNameCount}");
//
//         AssetDatabase.SaveAssets();
//         AssetDatabase.Refresh();
//         Debug.Log("Move contact updates complete.");
//     }
    //
    // [MenuItem("Tools/Fix Move Special")]
    // public static void FixMoveSpecial()
    // {
    //     string[] specialTypes = new string[]
    //     {
    //         "Fire", "Water", "Electric", "Grass", "Ice", "Psychic", "Dragon", "Dark"
    //     };
    //
    //     string folderPath = "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Moves/";
    //
    //     string[] guids = AssetDatabase.FindAssets("t:Move", new[] { folderPath });
    //
    //     foreach (string guid in guids)
    //     {
    //         string assetPath = AssetDatabase.GUIDToAssetPath(guid);
    //         Move move = AssetDatabase.LoadAssetAtPath<Move>(assetPath);
    //
    //         if (move != null)
    //         {
    //             move.isSpecial = false;
    //             if (specialTypes.Contains(move.type.typeName))
    //             {
    //                 move.isSpecial = true;
    //             }
    //             EditorUtility.SetDirty(move); // Mark as dirty for saving
    //             Debug.Log($"Updated special: {move.name}");
    //         }
    //     }
    //
    //     AssetDatabase.SaveAssets();
    //     AssetDatabase.Refresh();
    //     Debug.Log("Nature updates complete.");
    // }

    // [MenuItem("Tools/Fix Move Buff")]
    // public static void FixMoves()
    // {
    //     string folderPath = "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Moves/";
    //
    //     string[] guids = AssetDatabase.FindAssets("t:Move", new[] { folderPath });
    //
    //     foreach (string guid in guids)
    //     {
    //         string assetPath = AssetDatabase.GUIDToAssetPath(guid);
    //         Move move = AssetDatabase.LoadAssetAtPath<Move>(assetPath);
    //
    //         if (move != null)
    //         {
    //             ClearBuffs(move);
    //             EditorUtility.SetDirty(move); // Mark as dirty for saving
    //             Debug.Log($"Updated: {move.name}");
    //         }
    //     }
    //
    //     AssetDatabase.SaveAssets();
    //     AssetDatabase.Refresh();
    //     Debug.Log("Nature updates complete.");
    // }

    static void ClearBuffs(Move move)
    {
        move.buffOrDebuffData.Clear();
    }
    // private static void ApplyStatChanges(Nature nature)
    // {
    //     switch (nature.natureName.ToLower())
    //     {
    //     case "adamant":
    //         nature.statToIncrease = PokemonOperations.Stat.Attack;
    //         nature.statToDecrease = PokemonOperations.Stat.SpecialAttack;
    //         break;
    //     case "bashful":
    //         nature.statToIncrease = PokemonOperations.Stat.None;
    //         nature.statToDecrease = PokemonOperations.Stat.None;
    //         break;
    //     case "bold":
    //         nature.statToIncrease = PokemonOperations.Stat.Defense;
    //         nature.statToDecrease = PokemonOperations.Stat.Attack;
    //         break;
    //     case "brave":
    //         nature.statToIncrease = PokemonOperations.Stat.Attack;
    //         nature.statToDecrease = PokemonOperations.Stat.Speed;
    //         break;
    //     case "calm":
    //         nature.statToIncrease = PokemonOperations.Stat.SpecialDefense;
    //         nature.statToDecrease = PokemonOperations.Stat.Attack;
    //         break;
    //     case "careful":
    //         nature.statToIncrease = PokemonOperations.Stat.SpecialDefense;
    //         nature.statToDecrease = PokemonOperations.Stat.SpecialAttack;
    //         break;
    //     case "docile":
    //         nature.statToIncrease = PokemonOperations.Stat.None;
    //         nature.statToDecrease = PokemonOperations.Stat.None;
    //         break;
    //     case "gentle":
    //         nature.statToIncrease = PokemonOperations.Stat.SpecialDefense;
    //         nature.statToDecrease = PokemonOperations.Stat.Defense;
    //         break;
    //     case "hardy":
    //         nature.statToIncrease = PokemonOperations.Stat.None;
    //         nature.statToDecrease = PokemonOperations.Stat.None;
    //         break;
    //     case "hasty":
    //         nature.statToIncrease = PokemonOperations.Stat.Speed;
    //         nature.statToDecrease = PokemonOperations.Stat.Defense;
    //         break;
    //     case "impish":
    //         nature.statToIncrease = PokemonOperations.Stat.Defense;
    //         nature.statToDecrease = PokemonOperations.Stat.SpecialAttack;
    //         break;
    //     case "jolly":
    //         nature.statToIncrease = PokemonOperations.Stat.Speed;
    //         nature.statToDecrease = PokemonOperations.Stat.SpecialAttack;
    //         break;
    //     case "lax":
    //         nature.statToIncrease = PokemonOperations.Stat.Defense;
    //         nature.statToDecrease = PokemonOperations.Stat.SpecialDefense;
    //         break;
    //     case "lonely":
    //         nature.statToIncrease = PokemonOperations.Stat.Attack;
    //         nature.statToDecrease = PokemonOperations.Stat.Defense;
    //         break;
    //     case "mild":
    //         nature.statToIncrease = PokemonOperations.Stat.SpecialAttack;
    //         nature.statToDecrease = PokemonOperations.Stat.Defense;
    //         break;
    //     case "modest":
    //         nature.statToIncrease = PokemonOperations.Stat.SpecialAttack;
    //         nature.statToDecrease = PokemonOperations.Stat.Attack;
    //         break;
    //     case "naive":
    //         nature.statToIncrease = PokemonOperations.Stat.Speed;
    //         nature.statToDecrease = PokemonOperations.Stat.SpecialDefense;
    //         break;
    //     case "naughty":
    //         nature.statToIncrease = PokemonOperations.Stat.Attack;
    //         nature.statToDecrease = PokemonOperations.Stat.SpecialDefense;
    //         break;
    //     case "quiet":
    //         nature.statToIncrease = PokemonOperations.Stat.SpecialAttack;
    //         nature.statToDecrease = PokemonOperations.Stat.Speed;
    //         break;
    //     case "quirky":
    //         nature.statToIncrease = PokemonOperations.Stat.None;
    //         nature.statToDecrease = PokemonOperations.Stat.None;
    //         break;
    //     case "rash":
    //         nature.statToIncrease = PokemonOperations.Stat.SpecialAttack;
    //         nature.statToDecrease = PokemonOperations.Stat.SpecialDefense;
    //         break;
    //     case "relaxed":
    //         nature.statToIncrease = PokemonOperations.Stat.Defense;
    //         nature.statToDecrease = PokemonOperations.Stat.Speed;
    //         break;
    //     case "sassy":
    //         nature.statToIncrease = PokemonOperations.Stat.SpecialDefense;
    //         nature.statToDecrease = PokemonOperations.Stat.Speed;
    //         break;
    //     case "serious":
    //         nature.statToIncrease = PokemonOperations.Stat.None;
    //         nature.statToDecrease = PokemonOperations.Stat.None;
    //         break;
    //     case "timid":
    //         nature.statToIncrease = PokemonOperations.Stat.Speed;
    //         nature.statToDecrease = PokemonOperations.Stat.Attack;
    //         break;
    //     default:
    //         nature.statToIncrease = PokemonOperations.Stat.None;
    //         nature.statToDecrease = PokemonOperations.Stat.None;
    //         Debug.LogWarning($"Nature not matched: {nature.natureName}");
    //         break;
    //     }
    // }
}
//these belong in nameDB class when cleaning assets
    // public static bool GetMoveName(string nameMove)
    // {
    //     return moveNames.Any(s=>s.ToLower() == nameMove);
    // }
    // public static string GetMoveName(string nameMove)
    // {
    //     foreach (var nem in moveNames)
    //     {
    //         if (nem.ToLower() == nameMove)
    //         {
    //             return nem;
    //         }
    //     }
    //     return "Invalid name "+nameMove;
    // }
   // private static string[] moveNames =
    // {
    //     // 🐞 Bug-type
    //     "Fury Cutter",
    //     "Leech Life",
    //     "Silver Wind",
    //     "String Shot",
    //
    //     // 🐉 Dragon-type
    //     "Dragon Breath",
    //
    //     // ⚡ Electric-type
    //     "Thundershock",
    //     "Thunder Wave",
    //     "Thunderbolt",
    //     "Thunder",
    //
    //     // 🥋 Fighting-type
    //     "Brick Break",
    //     "Bulk Up",
    //     "Detect",
    //     "Double Kick",
    //     "Sky Uppercut",
    //
    //     // 🔥 Fire-type
    //     "Blaze Kick",
    //     "Ember",
    //     "Fire Punch",
    //     "Fire Spin",
    //     "Flamethrower",
    //
    //     // 🛫 Flying-type
    //     "Air Cutter",
    //     "Aerial Ace", // corrected spelling
    //     "Gust",
    //     "Mirror Move",
    //     "Peck",
    //     "Wing Attack",
    //
    //     // 🌿 Grass-type
    //     "Absorb",
    //     "Bullet Seed",
    //     "Giga Drain",
    //     "Leaf Blade",
    //     "Mega Drain",
    //     "Stun Spore",
    //
    //     // 🌍 Ground-type
    //     "Dig",
    //     "Earthquake",
    //     "Magnitude",
    //     "Mud-Slap",
    //     "Mud Shot",
    //     "Mud Sport",
    //     "Sand-Attack",
    //     "Sand Tomb",
    //
    //     // 🪨 Rock-type
    //     "Sandstorm",
    //
    //     // 💜 Normal-type
    //     "Attract",
    //     "Sonic Boom",
    //     "Harden",
    //     "Belly Drum",
    //     "Bide",
    //     "Covet",
    //     "Double Team",
    //     "Endeavor",
    //     "Foresight",
    //     "Focus Energy",
    //     "False Swipe",
    //     "Flail",
    //     "Fury Swipes",
    //     "Growl",
    //     "Headbutt",
    //     "Hyper Beam",
    //     "Leer",
    //     "Mean Look",
    //     "Morning Sun",
    //     "Moonlight",
    //     "Odor Sleuth",
    //     "Pound",
    //     "Protect",
    //     "Quick Attack",
    //     "Scratch",
    //     "Screech",
    //     "Slam",
    //     "Slash",
    //     "Supersonic",
    //     "Tail Whip",
    //     "Tackle",
    //     "Take Down",
    //     "Whirlwind",
    //
    //     // 💀 Poison-type
    //     "Poison Fang",
    //     "Poison Sting",
    //     "Toxic",
    //
    //     // 🔮 Psychic-type
    //     "Agility",
    //     "Confusion",
    //     "Light Screen",
    //     "Psybeam",
    //     "Reflect",
    //     "Rest",
    //
    //     // 🌊 Water-type
    //     "Hydro Pump",
    //     "Muddy Water",
    //     "Surf",
    //     "Water Gun",
    //     "Whirlpool",
    //
    //     // 👻 Ghost-type
    //     "Astonish",
    //     "Confuse Ray",
    //
    //     // ❄️ Ice-type
    //     "Haze",
    //
    //     // 🌑 Dark-type
    //     "Bite",
    //     "Crunch",
    //     "Faint Attack",
    //     "Pursuit"
    // };
