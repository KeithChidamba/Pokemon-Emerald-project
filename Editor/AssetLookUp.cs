using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AssetLookUp : EditorWindow
{
    [MenuItem("Tools/Check for moves that trap")]
    public static void CheckTrappingMoves()
    {
        Debug.Log($"Checking for traps");

        string folderPath = "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Moves/";
        string[] guids = AssetDatabase.FindAssets("t:Move", new[] { folderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Move move = AssetDatabase.LoadAssetAtPath<Move>(assetPath);

            if (move == null)
            {
                Debug.LogError($"[ERROR] Move asset not found at: {assetPath}");
                continue;
            }

            if (move.canTrap)
            {
                Debug.Log($"[TRAP EFFECT] {move.moveName}");
            }
         
        }
    }
}
