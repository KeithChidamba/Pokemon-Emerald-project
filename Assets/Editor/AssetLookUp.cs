using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

public class AssetLookUp : EditorWindow
{
    private const string MovesFolder = "Assets/Resources/Pokemon_project_assets/Pokemon_obj/Moves/";

    [MenuItem("Tools/Check for moves that trap")]
    public static void CheckTrappingMoves()
    {
        Debug.Log($"Checking for traps");
        
        string[] guids = AssetDatabase.FindAssets("t:Move", new[] { MovesFolder });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Move move = AssetDatabase.LoadAssetAtPath<Move>(assetPath);

            if (move is null)
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

    [MenuItem("Tools/List moves by group")]
    public static void ListMoveGroups()
    {
        string[] guids = AssetDatabase.FindAssets("t:Move", new[] { MovesFolder });
 
        var moves = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => (path, move: AssetDatabase.LoadAssetAtPath<Move>(path)))
            .Where(x =>
            {
                if (x.move != null) return true;
                Debug.LogError($"[ERROR] Move asset not found at: {x.path}");
                return false;
            })
            .Select(x => x.move)
            .ToList();
 
        var groups = moves
            .GroupBy(m => m.effectType)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.moveName).ToList());
 
        Debug.Log($"[MOVE GROUPS] {moves.Count} moves checked");
 
        // Iterate the enum so groups appear in enum order and empty ones are visible.
        foreach (EffectType effect in Enum.GetValues(typeof(EffectType)))
        {
            if (!groups.TryGetValue(effect, out var list))
            {
                Debug.Log($"[{effect}] (0)");
                continue;
            }
 
            var sb = new StringBuilder();
            sb.AppendLine($"[{effect}] ({list.Count})");
            foreach (var move in list)
                sb.AppendLine($"    {move.moveName}");
 
            // Passing the first move as context lets you click the log to ping it.
            Debug.Log(sb.ToString(), list[0]);
        }
    }
    
    private const string SearchFolder = "";
    [MenuItem("Tools/Check info modules for nulls")]
    public static void CheckInfoModules()
    {
        string[] guids = string.IsNullOrEmpty(SearchFolder)
            ? AssetDatabase.FindAssets("t:InfoModuleHolder")
            : AssetDatabase.FindAssets("t:InfoModuleHolder", new[] { SearchFolder });
 
        var checkedPerType = new Dictionary<string, int>();
        var problemsPerType = new Dictionary<string, int>();
 
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            InfoModuleHolder holder = AssetDatabase.LoadAssetAtPath<InfoModuleHolder>(assetPath);
 
            if (holder == null)
            {
                Debug.LogError($"[ERROR] InfoModuleHolder asset not found at: {assetPath}");
                continue;
            }
 
            string typeName = holder.GetType().Name;
            checkedPerType[typeName] = checkedPerType.GetValueOrDefault(typeName) + 1;
 
            var problems = new List<string>();
 
            // Dynamic modules ([SerializeReference]). A null entry here also appears
            // when the module's class was renamed, moved, or deleted.
            if (holder.dynamicInfoModules == null)
            {
                problems.Add("dynamicInfoModules list is null");
            }
            else
            {
                for (int i = 0; i < holder.dynamicInfoModules.Count; i++)
                {
                    if (holder.dynamicInfoModules[i] == null)
                        problems.Add($"dynamicInfoModules[{i}] is null");
                }
            }
 
            if (problems.Count == 0) continue;
 
            problemsPerType[typeName] = problemsPerType.GetValueOrDefault(typeName) + 1;
 
            var sb = new StringBuilder();
            sb.AppendLine($"[NULL MODULES] {typeName}: {holder.name} ({assetPath})");
            foreach (string problem in problems)
                sb.AppendLine($"    {problem}");
 
            // Passing the asset as context lets you click the log to ping it.
            Debug.LogWarning(sb.ToString(), holder);
        }
 
        // Summary per type
        var summary = new StringBuilder("[MODULE CHECK] Summary\n");
        foreach (var pair in checkedPerType.OrderBy(p => p.Key))
        {
            int bad = problemsPerType.GetValueOrDefault(pair.Key);
            summary.AppendLine($"    {pair.Key}: {pair.Value} checked, {bad} with null modules");
        }
        Debug.Log(summary.ToString());
    }


}
