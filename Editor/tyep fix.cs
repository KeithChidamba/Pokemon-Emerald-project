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
        List<Types> weaknesses = new();
        List<Types> resistances = new();
        List<Types> immunities = new();

        switch (typeSO.typeName.ToLower())
        {
            case "normal":
                weaknesses.Add(Types.Fighting);
                immunities.Add(Types.Ghost);
                break;

            case "fire":
                weaknesses.AddRange(new[] { Types.Water, Types.Ground, Types.Rock });
                resistances.AddRange(new[] { Types.Fire, Types.Grass, Types.Ice, Types.Bug, Types.Steel });
                break;

            case "water":
                weaknesses.AddRange(new[] { Types.Electric, Types.Grass });
                resistances.AddRange(new[] { Types.Fire, Types.Water, Types.Ice, Types.Steel });
                break;

            case "electric":
                weaknesses.Add(Types.Ground);
                resistances.AddRange(new[] { Types.Electric, Types.Flying, Types.Steel });
                break;

            case "grass":
                weaknesses.AddRange(new[] { Types.Fire, Types.Ice, Types.Poison, Types.Flying, Types.Bug });
                resistances.AddRange(new[] { Types.Water, Types.Electric, Types.Grass, Types.Ground });
                break;

            case "ice":
                weaknesses.AddRange(new[] { Types.Fire, Types.Fighting, Types.Rock, Types.Steel });
                resistances.Add(Types.Ice);
                break;

            case "fighting":
                weaknesses.AddRange(new[] { Types.Flying, Types.Psychic });
                resistances.AddRange(new[] { Types.Bug, Types.Rock, Types.Dark });
                break;

            case "poison":
                weaknesses.AddRange(new[] { Types.Ground, Types.Psychic });
                resistances.AddRange(new[] { Types.Grass, Types.Fighting, Types.Poison, Types.Bug });
                break;

            case "ground":
                weaknesses.AddRange(new[] { Types.Water, Types.Grass, Types.Ice });
                resistances.AddRange(new[] { Types.Poison, Types.Rock });
                immunities.Add(Types.Electric);
                break;

            case "flying":
                weaknesses.AddRange(new[] { Types.Electric, Types.Ice, Types.Rock });
                resistances.AddRange(new[] { Types.Grass, Types.Fighting, Types.Bug });
                immunities.Add(Types.Ground);
                break;

            case "psychic":
                weaknesses.AddRange(new[] { Types.Bug, Types.Ghost, Types.Dark });
                resistances.AddRange(new[] { Types.Fighting, Types.Psychic });
                break;

            case "bug":
                weaknesses.AddRange(new[] { Types.Fire, Types.Flying, Types.Rock });
                resistances.AddRange(new[] { Types.Grass, Types.Fighting, Types.Ground });
                break;

            case "rock":
                weaknesses.AddRange(new[] { Types.Water, Types.Grass, Types.Fighting, Types.Ground, Types.Steel });
                resistances.AddRange(new[] { Types.Normal, Types.Fire, Types.Poison, Types.Flying });
                break;

            case "ghost":
                weaknesses.AddRange(new[] { Types.Ghost, Types.Dark });
                resistances.AddRange(new[] { Types.Poison, Types.Bug });
                immunities.Add(Types.Normal);
                immunities.Add(Types.Fighting);
                break;

            case "dragon":
                weaknesses.AddRange(new[] { Types.Ice, Types.Dragon });
                resistances.AddRange(new[] { Types.Fire, Types.Water, Types.Electric, Types.Grass });
                break;

            case "dark":
                weaknesses.AddRange(new[] { Types.Fighting, Types.Bug });
                resistances.AddRange(new[] { Types.Ghost, Types.Dark });
                immunities.Add(Types.Psychic);
                break;

            case "steel":
                weaknesses.AddRange(new[] { Types.Fire, Types.Fighting, Types.Ground });
                resistances.AddRange(new[] {
                    Types.Normal, Types.Grass, Types.Ice,
                    Types.Flying, Types.Psychic, Types.Bug,
                    Types.Rock, Types.Dragon, Types.Steel
                });
                immunities.Add(Types.Poison);
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

