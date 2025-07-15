using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using System.IO;

public class NatureEVFixer : EditorWindow
{
    [MenuItem("Tools/Fix Pokemon Natures")]
    public static void FixNatures()
    {
        string folderPath = "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Natures/";

        string[] guids = AssetDatabase.FindAssets("t:Nature", new[] { folderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Nature nature = AssetDatabase.LoadAssetAtPath<Nature>(assetPath);

            if (nature != null)
            {
                ApplyStatChanges(nature);
                EditorUtility.SetDirty(nature); // Mark as dirty for saving
                Debug.Log($"Updated: {nature.name}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Nature updates complete.");
    }

    private static void ApplyStatChanges(Nature nature)
    {
        switch (nature.natureName.ToLower())
        {
        case "adamant":
            nature.statToIncrease = PokemonOperations.Stat.Attack;
            nature.statToDecrease = PokemonOperations.Stat.SpecialAttack;
            break;
        case "bashful":
            nature.statToIncrease = PokemonOperations.Stat.None;
            nature.statToDecrease = PokemonOperations.Stat.None;
            break;
        case "bold":
            nature.statToIncrease = PokemonOperations.Stat.Defense;
            nature.statToDecrease = PokemonOperations.Stat.Attack;
            break;
        case "brave":
            nature.statToIncrease = PokemonOperations.Stat.Attack;
            nature.statToDecrease = PokemonOperations.Stat.Speed;
            break;
        case "calm":
            nature.statToIncrease = PokemonOperations.Stat.SpecialDefense;
            nature.statToDecrease = PokemonOperations.Stat.Attack;
            break;
        case "careful":
            nature.statToIncrease = PokemonOperations.Stat.SpecialDefense;
            nature.statToDecrease = PokemonOperations.Stat.SpecialAttack;
            break;
        case "docile":
            nature.statToIncrease = PokemonOperations.Stat.None;
            nature.statToDecrease = PokemonOperations.Stat.None;
            break;
        case "gentle":
            nature.statToIncrease = PokemonOperations.Stat.SpecialDefense;
            nature.statToDecrease = PokemonOperations.Stat.Defense;
            break;
        case "hardy":
            nature.statToIncrease = PokemonOperations.Stat.None;
            nature.statToDecrease = PokemonOperations.Stat.None;
            break;
        case "hasty":
            nature.statToIncrease = PokemonOperations.Stat.Speed;
            nature.statToDecrease = PokemonOperations.Stat.Defense;
            break;
        case "impish":
            nature.statToIncrease = PokemonOperations.Stat.Defense;
            nature.statToDecrease = PokemonOperations.Stat.SpecialAttack;
            break;
        case "jolly":
            nature.statToIncrease = PokemonOperations.Stat.Speed;
            nature.statToDecrease = PokemonOperations.Stat.SpecialAttack;
            break;
        case "lax":
            nature.statToIncrease = PokemonOperations.Stat.Defense;
            nature.statToDecrease = PokemonOperations.Stat.SpecialDefense;
            break;
        case "lonely":
            nature.statToIncrease = PokemonOperations.Stat.Attack;
            nature.statToDecrease = PokemonOperations.Stat.Defense;
            break;
        case "mild":
            nature.statToIncrease = PokemonOperations.Stat.SpecialAttack;
            nature.statToDecrease = PokemonOperations.Stat.Defense;
            break;
        case "modest":
            nature.statToIncrease = PokemonOperations.Stat.SpecialAttack;
            nature.statToDecrease = PokemonOperations.Stat.Attack;
            break;
        case "naive":
            nature.statToIncrease = PokemonOperations.Stat.Speed;
            nature.statToDecrease = PokemonOperations.Stat.SpecialDefense;
            break;
        case "naughty":
            nature.statToIncrease = PokemonOperations.Stat.Attack;
            nature.statToDecrease = PokemonOperations.Stat.SpecialDefense;
            break;
        case "quiet":
            nature.statToIncrease = PokemonOperations.Stat.SpecialAttack;
            nature.statToDecrease = PokemonOperations.Stat.Speed;
            break;
        case "quirky":
            nature.statToIncrease = PokemonOperations.Stat.None;
            nature.statToDecrease = PokemonOperations.Stat.None;
            break;
        case "rash":
            nature.statToIncrease = PokemonOperations.Stat.SpecialAttack;
            nature.statToDecrease = PokemonOperations.Stat.SpecialDefense;
            break;
        case "relaxed":
            nature.statToIncrease = PokemonOperations.Stat.Defense;
            nature.statToDecrease = PokemonOperations.Stat.Speed;
            break;
        case "sassy":
            nature.statToIncrease = PokemonOperations.Stat.SpecialDefense;
            nature.statToDecrease = PokemonOperations.Stat.Speed;
            break;
        case "serious":
            nature.statToIncrease = PokemonOperations.Stat.None;
            nature.statToDecrease = PokemonOperations.Stat.None;
            break;
        case "timid":
            nature.statToIncrease = PokemonOperations.Stat.Speed;
            nature.statToDecrease = PokemonOperations.Stat.Attack;
            break;
        default:
            nature.statToIncrease = PokemonOperations.Stat.None;
            nature.statToDecrease = PokemonOperations.Stat.None;
            Debug.LogWarning($"Nature not matched: {nature.natureName}");
            break;
        }
    }
}

