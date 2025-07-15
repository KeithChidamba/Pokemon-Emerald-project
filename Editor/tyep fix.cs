using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TypeDataFiller : EditorWindow
{
    [MenuItem("Tools/Auto-Fill Type Data")]
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
                Debug.Log($"Updated type data for: {typeSO.typeName}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Type auto-fill completed.");
    }

    private static void ApplyTypeData(Type typeSO)
    {
        List<PokemonOperations.Types> weaknesses = new();
        List<PokemonOperations.Types> resistances = new();
        List<PokemonOperations.Types> immunities = new();

        switch (typeSO.typeName.ToLower())
        {
            case "normal":
                weaknesses.Add(PokemonOperations.Types.Fighting);
                immunities.Add(PokemonOperations.Types.Ghost);
                break;

            case "fire":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Water, PokemonOperations.Types.Ground, PokemonOperations.Types.Rock });
                resistances.AddRange(new[] { PokemonOperations.Types.Fire, PokemonOperations.Types.Grass, PokemonOperations.Types.Ice, PokemonOperations.Types.Bug, PokemonOperations.Types.Steel });
                break;

            case "water":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Electric, PokemonOperations.Types.Grass });
                resistances.AddRange(new[] { PokemonOperations.Types.Fire, PokemonOperations.Types.Water, PokemonOperations.Types.Ice, PokemonOperations.Types.Steel });
                break;

            case "electric":
                weaknesses.Add(PokemonOperations.Types.Ground);
                resistances.AddRange(new[] { PokemonOperations.Types.Electric, PokemonOperations.Types.Flying, PokemonOperations.Types.Steel });
                break;

            case "grass":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Fire, PokemonOperations.Types.Ice, PokemonOperations.Types.Poison, PokemonOperations.Types.Flying, PokemonOperations.Types.Bug });
                resistances.AddRange(new[] { PokemonOperations.Types.Water, PokemonOperations.Types.Electric, PokemonOperations.Types.Grass, PokemonOperations.Types.Ground });
                break;

            case "ice":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Fire, PokemonOperations.Types.Fighting, PokemonOperations.Types.Rock, PokemonOperations.Types.Steel });
                resistances.Add(PokemonOperations.Types.Ice);
                break;

            case "fighting":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Flying, PokemonOperations.Types.Psychic });
                resistances.AddRange(new[] { PokemonOperations.Types.Bug, PokemonOperations.Types.Rock, PokemonOperations.Types.Dark });
                break;

            case "poison":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Ground, PokemonOperations.Types.Psychic });
                resistances.AddRange(new[] { PokemonOperations.Types.Grass, PokemonOperations.Types.Fighting, PokemonOperations.Types.Poison, PokemonOperations.Types.Bug });
                break;

            case "ground":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Water, PokemonOperations.Types.Grass, PokemonOperations.Types.Ice });
                resistances.AddRange(new[] { PokemonOperations.Types.Poison, PokemonOperations.Types.Rock });
                immunities.Add(PokemonOperations.Types.Electric);
                break;

            case "flying":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Electric, PokemonOperations.Types.Ice, PokemonOperations.Types.Rock });
                resistances.AddRange(new[] { PokemonOperations.Types.Grass, PokemonOperations.Types.Fighting, PokemonOperations.Types.Bug });
                immunities.Add(PokemonOperations.Types.Ground);
                break;

            case "psychic":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Bug, PokemonOperations.Types.Ghost, PokemonOperations.Types.Dark });
                resistances.AddRange(new[] { PokemonOperations.Types.Fighting, PokemonOperations.Types.Psychic });
                break;

            case "bug":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Fire, PokemonOperations.Types.Flying, PokemonOperations.Types.Rock });
                resistances.AddRange(new[] { PokemonOperations.Types.Grass, PokemonOperations.Types.Fighting, PokemonOperations.Types.Ground });
                break;

            case "rock":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Water, PokemonOperations.Types.Grass, PokemonOperations.Types.Fighting, PokemonOperations.Types.Ground, PokemonOperations.Types.Steel });
                resistances.AddRange(new[] { PokemonOperations.Types.Normal, PokemonOperations.Types.Fire, PokemonOperations.Types.Poison, PokemonOperations.Types.Flying });
                break;

            case "ghost":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Ghost, PokemonOperations.Types.Dark });
                resistances.AddRange(new[] { PokemonOperations.Types.Poison, PokemonOperations.Types.Bug });
                immunities.Add(PokemonOperations.Types.Normal);
                immunities.Add(PokemonOperations.Types.Fighting);
                break;

            case "dragon":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Ice, PokemonOperations.Types.Dragon });
                resistances.AddRange(new[] { PokemonOperations.Types.Fire, PokemonOperations.Types.Water, PokemonOperations.Types.Electric, PokemonOperations.Types.Grass });
                break;

            case "dark":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Fighting, PokemonOperations.Types.Bug });
                resistances.AddRange(new[] { PokemonOperations.Types.Ghost, PokemonOperations.Types.Dark });
                immunities.Add(PokemonOperations.Types.Psychic);
                break;

            case "steel":
                weaknesses.AddRange(new[] { PokemonOperations.Types.Fire, PokemonOperations.Types.Fighting, PokemonOperations.Types.Ground });
                resistances.AddRange(new[] {
                    PokemonOperations.Types.Normal, PokemonOperations.Types.Grass, PokemonOperations.Types.Ice,
                    PokemonOperations.Types.Flying, PokemonOperations.Types.Psychic, PokemonOperations.Types.Bug,
                    PokemonOperations.Types.Rock, PokemonOperations.Types.Dragon, PokemonOperations.Types.Steel
                });
                immunities.Add(PokemonOperations.Types.Poison);
                break;

            default:
                Debug.LogWarning($"Unrecognized type: {typeSO.typeName}");
                break;
        }

        typeSO.weaknesses = weaknesses.ToArray();
        typeSO.resistances = resistances.ToArray();
        typeSO.immunities = immunities.ToArray();
    }
}

