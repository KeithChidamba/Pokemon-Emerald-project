using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TypeDataFiller : EditorWindow
{
    [MenuItem("Tools/Fill type data")]
    public static void FillTypeData()
    {
        string folderPath = "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Types/"; // Update to your types folder

        string[] guids = AssetDatabase.FindAssets("t:Type", new[] { folderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Type typeSO = AssetDatabase.LoadAssetAtPath<Type>(assetPath);

            if (typeSO != null)
            {
                ApplyTypeData(typeSO);
                EditorUtility.SetDirty(typeSO);
                Debug.Log($"Updated type data for: {typeSO.GetTypeName}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Type auto-fill completed.");
    }
    
    [MenuItem("Tools/Check Type Enum")]
    private static void CheckTypeEnum()
    {
        string folderPath = "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Types/"; // Update to your types folder

        string[] guids = AssetDatabase.FindAssets("t:Type", new[] { folderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Type typeSO = AssetDatabase.LoadAssetAtPath<Type>(assetPath);

            if (typeSO != null)
            {
                Debug.Log(typeSO.typeEnum+" for "+typeSO.name);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ApplyTypeData(Type typeSO)
    {
        List<PokemonType> weaknesses = new();
        List<PokemonType> resistances = new();
        List<PokemonType> immunities = new();

        switch (typeSO.GetTypeName.ToLower())
        {
            case "normal":
                weaknesses.Add(PokemonType.Fighting);
                immunities.Add(PokemonType.Ghost);
                break;

            case "fire":
                weaknesses.AddRange(new[] { PokemonType.Water, PokemonType.Ground, PokemonType.Rock });
                resistances.AddRange(new[] { PokemonType.Fire, PokemonType.Grass, PokemonType.Ice, PokemonType.Bug, PokemonType.Steel });
                break;

            case "water":
                weaknesses.AddRange(new[] { PokemonType.Electric, PokemonType.Grass });
                resistances.AddRange(new[] { PokemonType.Fire, PokemonType.Water, PokemonType.Ice, PokemonType.Steel });
                break;

            case "electric":
                weaknesses.Add(PokemonType.Ground);
                resistances.AddRange(new[] { PokemonType.Electric, PokemonType.Flying, PokemonType.Steel });
                break;

            case "grass":
                weaknesses.AddRange(new[] { PokemonType.Fire, PokemonType.Ice, PokemonType.Poison, PokemonType.Flying, PokemonType.Bug });
                resistances.AddRange(new[] { PokemonType.Water, PokemonType.Electric, PokemonType.Grass, PokemonType.Ground });
                break;

            case "ice":
                weaknesses.AddRange(new[] { PokemonType.Fire, PokemonType.Fighting, PokemonType.Rock, PokemonType.Steel });
                resistances.Add(PokemonType.Ice);
                break;

            case "fighting":
                weaknesses.AddRange(new[] { PokemonType.Flying, PokemonType.Psychic });
                resistances.AddRange(new[] { PokemonType.Bug, PokemonType.Rock, PokemonType.Dark });
                break;

            case "poison":
                weaknesses.AddRange(new[] { PokemonType.Ground, PokemonType.Psychic });
                resistances.AddRange(new[] { PokemonType.Grass, PokemonType.Fighting, PokemonType.Poison, PokemonType.Bug });
                break;

            case "ground":
                weaknesses.AddRange(new[] { PokemonType.Water, PokemonType.Grass, PokemonType.Ice });
                resistances.AddRange(new[] { PokemonType.Poison, PokemonType.Rock });
                immunities.Add(PokemonType.Electric);
                break;

            case "flying":
                weaknesses.AddRange(new[] { PokemonType.Electric, PokemonType.Ice, PokemonType.Rock });
                resistances.AddRange(new[] { PokemonType.Grass, PokemonType.Fighting, PokemonType.Bug });
                immunities.Add(PokemonType.Ground);
                break;

            case "psychic":
                weaknesses.AddRange(new[] { PokemonType.Bug, PokemonType.Ghost, PokemonType.Dark });
                resistances.AddRange(new[] { PokemonType.Fighting, PokemonType.Psychic });
                break;

            case "bug":
                weaknesses.AddRange(new[] { PokemonType.Fire, PokemonType.Flying, PokemonType.Rock });
                resistances.AddRange(new[] { PokemonType.Grass, PokemonType.Fighting, PokemonType.Ground });
                break;

            case "rock":
                weaknesses.AddRange(new[] { PokemonType.Water, PokemonType.Grass, PokemonType.Fighting, PokemonType.Ground, PokemonType.Steel });
                resistances.AddRange(new[] { PokemonType.Normal, PokemonType.Fire, PokemonType.Poison, PokemonType.Flying });
                break;

            case "ghost":
                weaknesses.AddRange(new[] { PokemonType.Ghost, PokemonType.Dark });
                resistances.AddRange(new[] { PokemonType.Poison, PokemonType.Bug });
                immunities.Add(PokemonType.Normal);
                immunities.Add(PokemonType.Fighting);
                break;

            case "dragon":
                weaknesses.AddRange(new[] { PokemonType.Ice, PokemonType.Dragon });
                resistances.AddRange(new[] { PokemonType.Fire, PokemonType.Water, PokemonType.Electric, PokemonType.Grass });
                break;

            case "dark":
                weaknesses.AddRange(new[] { PokemonType.Fighting, PokemonType.Bug });
                resistances.AddRange(new[] { PokemonType.Ghost, PokemonType.Dark });
                immunities.Add(PokemonType.Psychic);
                break;

            case "steel":
                weaknesses.AddRange(new[] { PokemonType.Fire, PokemonType.Fighting, PokemonType.Ground });
                resistances.AddRange(new[] {
                    PokemonType.Normal, PokemonType.Grass, PokemonType.Ice,
                    PokemonType.Flying, PokemonType.Psychic, PokemonType.Bug,
                    PokemonType.Rock, PokemonType.Dragon, PokemonType.Steel
                });
                immunities.Add(PokemonType.Poison);
                break;

            default:
                Debug.LogWarning($"Unrecognized type: {typeSO.GetTypeName}");
                break;
        }

        typeSO.weaknesses = weaknesses.ToArray();
        typeSO.resistances = resistances.ToArray();
        typeSO.immunities = immunities.ToArray();
    }
}

