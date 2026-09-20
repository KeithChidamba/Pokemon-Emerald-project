// SoundManagerAutoPopulate.cs
//
// Editor-only tool that scans your sound folder and fills in SoundManager's
// serialized lists automatically, instead of dragging 60+ clips by hand.
//
// It works off two things:
//   1. A filename -> enum mapping table below, built from the exact file
//      names in your project (both the numbered SFX pack and the OST tracks).
//   2. A rule for what layer/loop/fade each kind of sound gets:
//        - OST tracks (music)      -> MusicId,  loop = true,  fadeSeconds = 1.0
//        - Fanfares/jingles        -> JingleId,  loop = false, fadeSeconds = 0
//        - Menu/UI one-shots       -> UiId,      loop = false, called once
//        - Everything else (SFX)   -> SfxId,     loop = false, called once
//
// This mirrors how Emerald itself works: BGM tracks loop until something else
// replaces them; a "Level Up!"/"Obtained a Badge!" jingle is a short one-shot;
// every UI click and overworld/battle SFX just fires once per event.
//
// Usage:
//   1. Put this file in a folder named "Editor" anywhere under Assets
//      (Unity requires editor scripts to live in an "Editor" folder).
//   2. Select your SoundManager GameObject/prefab in the Inspector.
//   3. Menu bar: Tools > Sound Manager > Auto-Populate From Folder.
//   4. Point it at your sound folder when prompted (defaults to the path
//      your files are already in) and it fills musicSounds/jingleSounds/
//      uiSounds/sfxSounds via reflection, matching by filename.
//
// Not compiled/tested here (no Unity install in this environment). If a
// menu item or reflection call doesn't match your Unity version, tell me
// the error and I'll adjust it.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SoundManagerAutoPopulate
{
    // ------------------------------------------------------------------
    // Filename -> enum mapping.
    // Key: the file name WITHOUT extension, exactly as it appears on disk.
    // Value: (layer, enum name as a string, loop override or null = use layer default)
    // ------------------------------------------------------------------
    private class MapEntry
    {
        public SoundLayer Layer;
        public string EnumName;
        public bool? LoopOverride; // null = use the layer's default loop behaviour

        public MapEntry(SoundLayer layer, string enumName, bool? loopOverride = null)
        {
            Layer = layer;
            EnumName = enumName;
            LoopOverride = loopOverride;
        }
    }

    private static readonly Dictionary<string, MapEntry> FileMap = new Dictionary<string, MapEntry>
    {
        // ---------------- Music (OST tracks) — always loop ----------------
        ["1-05 - Littleroot Town"] = new MapEntry(SoundLayer.Music, "MusicId.Littleroot"),
        ["1-06 - Birch Pokémon Lab"] = new MapEntry(SoundLayer.Music, "MusicId.BirchLab"),
        ["1-07. May"] = new MapEntry(SoundLayer.Music, "MusicId.May"),
        ["1-09 - Battle! (Wild Pokémon)"] = new MapEntry(SoundLayer.Music, "MusicId.BattleWild"),
        ["1-10 - Victory! (Wild Pokémon)"] = new MapEntry(SoundLayer.Music, "MusicId.VictoryWild", loopOverride: false), // victory fanfares don't loop
        ["1-11 - Route 101"] = new MapEntry(SoundLayer.Music, "MusicId.Route101"),
        ["1-12 - Oldale Town"] = new MapEntry(SoundLayer.Music, "MusicId.OldaleTown"),
        ["1-13 - Pokémon Center"] = new MapEntry(SoundLayer.Music, "MusicId.PokemonCenter"),
        ["1-15 - Trainers' Eyes Meet (Youngster)"] = new MapEntry(SoundLayer.Music, "MusicId.TrainerEyesMeetYoungster"),
        ["1-16 - Trainers' Eyes Meet (Lass)"] = new MapEntry(SoundLayer.Music, "MusicId.TrainerEyesMeetLass"),
        ["1-17 - Battle! (Trainer Battle)"] = new MapEntry(SoundLayer.Music, "MusicId.BattleTrainer"),
        ["1-18 - Victory! (Trainer Battle)"] = new MapEntry(SoundLayer.Music, "MusicId.VictoryTrainer", loopOverride: false),
        ["1-20 - Petalburg City"] = new MapEntry(SoundLayer.Music, "MusicId.PetalburgCity"),
        ["1-22 - Route 104"] = new MapEntry(SoundLayer.Music, "MusicId.Route104"),

        // ---------------- Jingles — short one-shots, don't loop ----------------
        ["1-14 - Pokémon Healed"] = new MapEntry(SoundLayer.Jingle, "JingleId.Healed", loopOverride: false),
        ["1-19 - Level Up!"] = new MapEntry(SoundLayer.Jingle, "JingleId.LevelUp", loopOverride: false),
        ["0021 - EXP Gain"] = new MapEntry(SoundLayer.Sfx, "SfxId.ExpGain", loopOverride: false),

        // ---------------- UI (menus, PC, Pokédex, PokéNav, bag) — one-shot ----------------
        ["0005 - Select Cursor"] = new MapEntry(SoundLayer.UI, "UiId.Select", loopOverride: false),
        ["0006 - Window Open"] = new MapEntry(SoundLayer.UI, "UiId.WindowOpen", loopOverride: false),
        ["0024 - Click Mechanical"] = new MapEntry(SoundLayer.UI, "UiId.ClickMechanical", loopOverride: false),
        ["0016 - Bad Error Buzz"] = new MapEntry(SoundLayer.UI, "UiId.Error", loopOverride: false),
        ["0020 - Failure"] = new MapEntry(SoundLayer.UI, "UiId.Failure", loopOverride: false),
        ["0015 - Alert Exclamation"] = new MapEntry(SoundLayer.UI, "UiId.Alert", loopOverride: false),
        ["001F - Success (catch confirm guess)"] = new MapEntry(SoundLayer.UI, "UiId.Success", loopOverride: false),
        ["0001 - Use Item Heal"] = new MapEntry(SoundLayer.UI, "UiId.UseItem", loopOverride: false),
        ["0037 - Save"] = new MapEntry(SoundLayer.UI, "UiId.Save", loopOverride: false),
        ["005F - Shop"] = new MapEntry(SoundLayer.UI, "UiId.Shop", loopOverride: false),
        ["0002 - PC Login"] = new MapEntry(SoundLayer.UI, "UiId.PcLogin", loopOverride: false),
        ["0004 - PC On"] = new MapEntry(SoundLayer.UI, "UiId.PcOn", loopOverride: false),
        ["0003 - PC Off"] = new MapEntry(SoundLayer.UI, "UiId.PcOff", loopOverride: false),
        ["006C - Pokedex Scroll"] = new MapEntry(SoundLayer.UI, "UiId.PokedexScroll", loopOverride: false),
        ["006D - Pokedex Page Turn"] = new MapEntry(SoundLayer.UI, "UiId.PokedexPageTurn", loopOverride: false),
        ["006E - PokeNav On"] = new MapEntry(SoundLayer.UI, "UiId.PokeNavOn", loopOverride: false),
        ["006F - PokeNav Off"] = new MapEntry(SoundLayer.UI, "UiId.PokeNavOff", loopOverride: false),
        ["00FC - FRLG Bag Cursor"] = new MapEntry(SoundLayer.UI, "UiId.BagCursor", loopOverride: false),
        ["00FD - FRLG Bag Pocket Switch"] = new MapEntry(SoundLayer.UI, "UiId.BagPocketSwitch", loopOverride: false),
        ["00FE - FRLG Ball Click"] = new MapEntry(SoundLayer.UI, "UiId.BallClick", loopOverride: false),
        ["00FF - FRLG Shop"] = new MapEntry(SoundLayer.UI, "UiId.FrlgShop", loopOverride: false),
        ["0101 - FRLG Help Open"] = new MapEntry(SoundLayer.UI, "UiId.HelpOpen", loopOverride: false),
        ["0102 - FRLG Help Close"] = new MapEntry(SoundLayer.UI, "UiId.HelpClose", loopOverride: false),
        ["0103 - FRLG Help Error"] = new MapEntry(SoundLayer.UI, "UiId.HelpError", loopOverride: false),

        // ---------------- SFX (overworld / battle) — one-shot, many voices ----------------
        ["0007 - Wall Bump"] = new MapEntry(SoundLayer.Sfx, "SfxId.WallBump", loopOverride: false),
        ["0008 - Door"] = new MapEntry(SoundLayer.Sfx, "SfxId.Door", loopOverride: false),
        ["0009 - Exit Stairs"] = new MapEntry(SoundLayer.Sfx, "SfxId.ExitStairs", loopOverride: false),
        ["0012 - Sliding Door"] = new MapEntry(SoundLayer.Sfx, "SfxId.SlidingDoor", loopOverride: false),
        ["000A - Ledge Jump"] = new MapEntry(SoundLayer.Sfx, "SfxId.LedgeJump", loopOverride: false),
        ["000B - Bike Bell"] = new MapEntry(SoundLayer.Sfx, "SfxId.BikeBell", loopOverride: false),
        ["0022 - Bike Hop"] = new MapEntry(SoundLayer.Sfx, "SfxId.BikeHop", loopOverride: false),
        ["002C - Unlock"] = new MapEntry(SoundLayer.Sfx, "SfxId.Unlock", loopOverride: false),
        ["002D - Warp In"] = new MapEntry(SoundLayer.Sfx, "SfxId.WarpIn", loopOverride: false),
        ["002E - Warp Out"] = new MapEntry(SoundLayer.Sfx, "SfxId.WarpOut", loopOverride: false),
        ["004F - Poison Damage Overworld"] = new MapEntry(SoundLayer.Sfx, "SfxId.PoisonDamageOverworld", loopOverride: false),
        ["0048 - Itemfinder"] = new MapEntry(SoundLayer.Sfx, "SfxId.Itemfinder", loopOverride: false),
        ["0049 - Ding Dong"] = new MapEntry(SoundLayer.Sfx, "SfxId.DingDong", loopOverride: false),

        ["003D - Ball Throw"] = new MapEntry(SoundLayer.Sfx, "SfxId.BallThrow", loopOverride: false),
        ["000F - Ball Open (release recall guess)"] = new MapEntry(SoundLayer.Sfx, "SfxId.BallOpen", loopOverride: false),
        ["003C - Ball Pull In Trade"] = new MapEntry(SoundLayer.Sfx, "SfxId.BallPullInTrade", loopOverride: false),
        ["0038 - Ball Bounce 1"] = new MapEntry(SoundLayer.Sfx, "SfxId.BallBounce1", loopOverride: false),
        ["0039 - Ball Bounce 2"] = new MapEntry(SoundLayer.Sfx, "SfxId.BallBounce2", loopOverride: false),
        ["003A - Ball Bounce 3"] = new MapEntry(SoundLayer.Sfx, "SfxId.BallBounce3", loopOverride: false),
        ["003B - Ball Bounce 4"] = new MapEntry(SoundLayer.Sfx, "SfxId.BallBounce4", loopOverride: false),
        ["0017 - Ball Shake"] = new MapEntry(SoundLayer.Sfx, "SfxId.BallShake", loopOverride: false),

        ["0011 - Flee"] = new MapEntry(SoundLayer.Sfx, "SfxId.Flee", loopOverride: false),
        ["0010 - Faint"] = new MapEntry(SoundLayer.Sfx, "SfxId.Faint", loopOverride: false),
        ["005A - Low Health Beep"] = new MapEntry(SoundLayer.Sfx, "SfxId.LowHealthBeep", loopOverride: false),
        ["0066 - Shiny Sparkle"] = new MapEntry(SoundLayer.Sfx, "SfxId.ShinySparkle", loopOverride: false),
        ["0071 - Egg Hatch"] = new MapEntry(SoundLayer.Sfx, "SfxId.EggHatch", loopOverride: false),
        ["000C - Hit Not Very Effective"] = new MapEntry(SoundLayer.Sfx, "SfxId.HitNotVeryEffective", loopOverride: false),
        ["000D - Hit Normal"] = new MapEntry(SoundLayer.Sfx, "SfxId.HitNormal", loopOverride: false),
        ["000E - Hit Super Effective"] = new MapEntry(SoundLayer.Sfx, "SfxId.HitSuperEffective", loopOverride: false),
        ["00EF - Stat Increase"] = new MapEntry(SoundLayer.Sfx, "SfxId.StatIncrease", loopOverride: false),
        ["00F5 - Stat Decrease"] = new MapEntry(SoundLayer.Sfx, "SfxId.StatDecrease", loopOverride: false),

        ["008A - Status Paralysis (Thunder Wave guess)"] = new MapEntry(SoundLayer.Sfx, "SfxId.StatusParalysis", loopOverride: false),
        ["0094 - Status Poison (Toxic guess)"] = new MapEntry(SoundLayer.Sfx, "SfxId.StatusPoison", loopOverride: false),
        ["0092 - Status Burn (Flamethrower guess)"] = new MapEntry(SoundLayer.Sfx, "SfxId.StatusBurn", loopOverride: false),
        ["0097 - Status Burn Alt (Ember guess)"] = new MapEntry(SoundLayer.Sfx, "SfxId.StatusBurnAlt", loopOverride: false),
    };

    // File systems and download tools disagree on how to store accented characters:
    // "é" can be one character (NFC) or "e" + a combining accent (NFD). They look
    // identical but don't compare equal, which made every "Pokémon" filename miss.
    // Normalizing both the table keys and the scanned file names to NFC fixes that.
    private static string Norm(string s) => s.Normalize(NormalizationForm.FormC);

    private static readonly Dictionary<string, MapEntry> NormalizedFileMap = BuildNormalizedMap();

    private static Dictionary<string, MapEntry> BuildNormalizedMap()
    {
        var map = new Dictionary<string, MapEntry>();
        foreach (var kv in FileMap)
            map[Norm(kv.Key)] = kv.Value;
        return map;
    }

    // Per-layer default loop/fade behaviour, matching how the game actually uses each layer.
    private static bool DefaultLoopFor(SoundLayer layer) => layer == SoundLayer.Music;
    private static float DefaultFadeFor(SoundLayer layer) => layer == SoundLayer.Music ? 1.0f : 0f;

    [MenuItem("Tools/Sound Manager/Auto-Populate From Folder")]
    private static void AutoPopulate()
    {
        var manager = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<SoundManager>()
            : null;

        if (manager == null)
        {
            EditorUtility.DisplayDialog("Sound Manager Auto-Populate",
                "Select a GameObject with a SoundManager component first.", "OK");
            return;
        }

        string folder = EditorUtility.OpenFolderPanel(
            "Select sound folder (must be inside Assets)",
            "Assets/Resources/Pokemon_project_assets/Sound", "");

        if (string.IsNullOrEmpty(folder)) return;

        if (!folder.Replace('\\', '/').Contains("/Assets/"))
        {
            EditorUtility.DisplayDialog("Sound Manager Auto-Populate",
                "That folder isn't inside this project's Assets folder. Pick a folder under Assets so Unity can load the clips as assets.", "OK");
            return;
        }

        // Convert the absolute OS path back to a project-relative "Assets/..." path.
        string assetsRelative = "Assets" + folder.Replace('\\', '/').Split(new[] { "/Assets" }, StringSplitOptions.None)[1];

        var audioFiles = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".flac", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();

        int matched = 0, skipped = 0;
        var skippedNames = new List<string>();

        // Group results per layer so we can build each serialized list in one pass.
        var musicResults = new List<(string enumName, AudioClip clip, bool loop, float fade)>();
        var jingleResults = new List<(string enumName, AudioClip clip, bool loop, float fade)>();
        var uiResults = new List<(string enumName, AudioClip clip, bool loop, float fade)>();
        var sfxResults = new List<(string enumName, AudioClip clip, bool loop, float fade)>();

        foreach (var filePath in audioFiles)
        {
            string nameNoExt = Path.GetFileNameWithoutExtension(filePath);

            if (!NormalizedFileMap.TryGetValue(Norm(nameNoExt), out var entry))
            {
                skipped++;
                skippedNames.Add(nameNoExt);
                continue;
            }

            string relativeAssetPath = assetsRelative + "/" + Path.GetFileName(filePath);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativeAssetPath);
            if (clip == null)
            {
                Debug.LogWarning($"[SoundManagerAutoPopulate] Could not load AudioClip at {relativeAssetPath}. Is it imported yet?");
                skipped++;
                skippedNames.Add(nameNoExt);
                continue;
            }

            bool loop = entry.LoopOverride ?? DefaultLoopFor(entry.Layer);
            float fade = DefaultFadeFor(entry.Layer);

            switch (entry.Layer)
            {
                case SoundLayer.Music: musicResults.Add((entry.EnumName, clip, loop, fade)); break;
                case SoundLayer.Jingle: jingleResults.Add((entry.EnumName, clip, loop, fade)); break;
                case SoundLayer.UI: uiResults.Add((entry.EnumName, clip, loop, fade)); break;
                case SoundLayer.Sfx: sfxResults.Add((entry.EnumName, clip, loop, fade)); break;
            }
            matched++;
        }

        Undo.RecordObject(manager, "Auto-Populate Sound Manager");

        PopulateList(manager, "musicSounds", typeof(MusicId), musicResults);
        PopulateList(manager, "jingleSounds", typeof(JingleId), jingleResults);
        PopulateList(manager, "uiSounds", typeof(UiId), uiResults);
        PopulateList(manager, "sfxSounds", typeof(SfxId), sfxResults);

        EditorUtility.SetDirty(manager);

        string summary = $"Matched {matched} file(s). Skipped {skipped}.";
        if (skippedNames.Count > 0)
            summary += "\n\nSkipped (no mapping found or clip failed to load):\n" + string.Join("\n", skippedNames);

        File.WriteAllText(Path.Combine("Assets/Resources",
            DirectoryHandler.GetDirectory(AssetDirectory.TestLogs),"output.txt"),"[SoundManagerAutoPopulate] " + summary);
    }

    /// <summary>
    /// Builds a new list of SoundEntry&lt;TEnum&gt; via reflection (since SoundEntry&lt;T&gt;
    /// is generic and the enum type differs per layer) and assigns it to the named
    /// private field on the manager, replacing whatever was there before.
    /// Existing entries not present in the scanned folder are left out — re-run after
    /// adding more files, or hand-edit the Inspector afterward for anything extra.
    /// </summary>
    private static void PopulateList(
        SoundManager manager,
        string fieldName,
        Type enumType,
        List<(string enumName, AudioClip clip, bool loop, float fade)> results)
    {
        var entryType = typeof(SoundEntry<>).MakeGenericType(enumType);
        var listType = typeof(List<>).MakeGenericType(entryType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType);

        foreach (var r in results)
        {
            // "MusicId.Littleroot" -> enum value MusicId.Littleroot
            string valueName = r.enumName.Contains(".") ? r.enumName.Split('.')[1] : r.enumName;
            object enumValue;
            try
            {
                enumValue = Enum.Parse(enumType, valueName);
            }
            catch (ArgumentException)
            {
                Debug.LogWarning($"[SoundManagerAutoPopulate] Enum value '{valueName}' not found on {enumType.Name}. Skipping.");
                continue;
            }

            var soundData = new SoundData
            {
                clips = new[] { r.clip },
                volume = 1f,
                pitchVariance = enumType == typeof(SfxId) ? 0.03f : 0f, // slight variance for repeatable SFX only
                fadeSeconds = r.fade,
                loop = r.loop,
                duckMusicInstead = true,
            };

            var entry = Activator.CreateInstance(entryType);
            entryType.GetField("id").SetValue(entry, enumValue);
            entryType.GetField("data").SetValue(entry, soundData);

            list.Add(entry);
        }

        var field = typeof(SoundManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Debug.LogError($"[SoundManagerAutoPopulate] Could not find field '{fieldName}' on SoundManager. Did the field name change?");
            return;
        }
        field.SetValue(manager, list);
    }
}
#endif