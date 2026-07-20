using System.Collections;
using System.Collections.Generic;
using System.IO;

public enum AssetDirectory
{ 
    Status, Moves, Abilities, Types, Natures, Pokemon, PokemonImage, UI, ItemUI, Items
    ,AdditionalInfo,BerryTreeData,PokeMartData,TrainerData,PokemonPartyImage,StoryObjectiveData
    ,OverworldItemPickups,Tests,TestAssets,TestLogs
}
public enum SaveDataDirectory
{
    Items, HeldItems, StorageItems, StoragePokemon, PartyPokemon, Player,
    PCStorage, Overworld, StoryObjectives, BerryTrees,
    GameSettings,OverworldItemPickupRegistry
}

public class DirectoryHandler
{
    private const string RootAssetDirectory = "Pokemon_project_assets/";
    
    public static readonly Dictionary<AssetDirectory, string> AssetDirectories = new()
    {
        {AssetDirectory.Moves,"Pokemon_obj/Moves/" },
        {AssetDirectory.Status,"Pokemon_obj/Status/" },
        {AssetDirectory.Pokemon,"Pokemon_obj/Pokemon/" },
        {AssetDirectory.PokemonImage,"pokemon_img/" },
        {AssetDirectory.PokemonPartyImage,"pokemon_img/party_img/"},
        {AssetDirectory.Abilities,"Pokemon_obj/Abilities/" },
        {AssetDirectory.Types,"Pokemon_obj/Types/" },
        {AssetDirectory.Natures,"Pokemon_obj/Natures/" },
        {AssetDirectory.UI,"UI/" },
        {AssetDirectory.ItemUI,"UI/Item_images/" },
        {AssetDirectory.Items,"Items/" },
        {AssetDirectory.AdditionalInfo,"Items/AdditionalInfo/" },
        {AssetDirectory.BerryTreeData,"Overwolrd_obj/Interactions/Berry Trees/Berry Data/"},
        {AssetDirectory.StoryObjectiveData,"Overwolrd_obj/Story Objectives/"},
        {AssetDirectory.PokeMartData,"Overwolrd_obj/Poke_Mart_Data"},
        {AssetDirectory.OverworldItemPickups,"Overwolrd_obj/Interactions/Overworld_Pickups"},
        {AssetDirectory.TrainerData,"Enemies/Data/"},
        { AssetDirectory.Tests,"Tests/"},
        { AssetDirectory.TestLogs,"Tests/Logs/"},
        {AssetDirectory.TestAssets,"Tests/Assets/"}
    };
    public static readonly Dictionary<SaveDataDirectory, string> SaveDataDirectories = new()
    {
        { SaveDataDirectory.Items, "/Items" },
        { SaveDataDirectory.HeldItems, "/Items/Held_Items" },
        { SaveDataDirectory.StorageItems, "/Items/Storage_Items" },
        { SaveDataDirectory.StoragePokemon, "/Storage_Pokemon" },
        { SaveDataDirectory.PartyPokemon, "/Party_Pokemon" },
        { SaveDataDirectory.Player, "/Player" },
        { SaveDataDirectory.PCStorage, "/PC_Storage" },
        { SaveDataDirectory.Overworld, "/Overworld" },
        { SaveDataDirectory.StoryObjectives, "/Overworld/Story_Objectives" },
        { SaveDataDirectory.GameSettings,"/GameSettings"},
        { SaveDataDirectory.BerryTrees, "/Overworld/Berry_Trees" },
        { SaveDataDirectory.OverworldItemPickupRegistry,"/Overworld/Item_Pickups"}
    };

    public static string GetSaveDirectory(SaveDataDirectory directoryKey)
    {
        return SaveDataDirectories[directoryKey]+"/";
    }

    public static string GetDirectory(AssetDirectory directoryKey)
    {
        return RootAssetDirectory + AssetDirectories[directoryKey];
    }
    public static IEnumerator CopyDirectoryFiles(string sourceDir, string destinationDir, bool recursive)
    {
        // Ensure source exists
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        // Create destination if it doesn’t exist
        Directory.CreateDirectory(destinationDir);

        // Copy files
        foreach (string filePath in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(filePath);
            string destPath = Path.Combine(destinationDir, fileName);
            File.Copy(filePath, destPath, overwrite: true); // overwrite: true to replace existing files
        }

        // Copy subdirectories if recursive
        if (recursive)
        {
            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(directory);
                string destSubDir = Path.Combine(destinationDir, dirName);
                yield return CopyDirectoryFiles(directory, destSubDir, true);
            }
        }

    }
}
