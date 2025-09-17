using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Serialization;
using UnityEngine.UI;


public class Save_manager : MonoBehaviour
{
    [DllImport("__Internal")] private static extern void DownloadZipAndStoreLocally();
    [DllImport("__Internal")] private static extern void CreateDirectories();
    [DllImport("__Internal")] private static extern void UploadZipAndStoreToIDBFS();
    [FormerlySerializedAs("party_IDs")] public List<string> partyIDs;
    public Area_manager area;
    public static Save_manager Instance { get; private set; }
    private string _saveDataPath = "Assets/Save_data";
    private string _tempSaveDataPath = "Assets/Temp_Save_data";
    private event Action<string,Exception> OnSaveDataFail;
    public event Func<IEnumerator> OnPlayerDataSaved;
    public event Action OnOverworldDataLoaded;
    public enum AssetDirectory
    { 
        Status, Moves, Abilities, Types, Natures, Pokemon, PokemonImage, UI, Items, MartItems, NonMartItems
        ,AdditionalInfo,Berries
    };
    
    private static readonly Dictionary<AssetDirectory, string> Directories = new()
    {
        {AssetDirectory.Moves,"Pokemon_project_assets/Pokemon_obj/Moves/" },
        {AssetDirectory.Status,"Pokemon_project_assets/Pokemon_obj/Status/" },
        {AssetDirectory.Pokemon,"Pokemon_project_assets/Pokemon_obj/Pokemon/" },
        {AssetDirectory.PokemonImage,"Pokemon_project_assets/pokemon_img/" },
        {AssetDirectory.Abilities,"Pokemon_project_assets/Pokemon_obj/Abilities/" },
        {AssetDirectory.Types,"Pokemon_project_assets/Pokemon_obj/Types/" },
        {AssetDirectory.Natures,"Pokemon_project_assets/Pokemon_obj/Natures/" },
        {AssetDirectory.UI,"Pokemon_project_assets/UI/" },
        {AssetDirectory.NonMartItems,"Pokemon_project_assets/Items/NonMartItems/" },
        {AssetDirectory.MartItems,"Pokemon_project_assets/Items/Mart_Items/" },
        {AssetDirectory.Items,"Pokemon_project_assets/Items/" },
        {AssetDirectory.Berries,"Pokemon_project_assets/Items/Berries/" },
        {AssetDirectory.AdditionalInfo,"Pokemon_project_assets/Items/AdditionalInfo/" }
    };
    public static string GetDirectory(AssetDirectory directory)
    {
        return Directories[directory];
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        OnSaveDataFail += HandleSaveError;
        _saveDataPath = GetSavePath();
        CreateTemporaryDirectory();
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            Game_Load.Instance.uploadButton.interactable = false;
            LoadPlayerData(); 
            LoadItemData();
            LoadPokemonData();
            LoadOverworldData();
        }
        else
        {
            Game_Load.Instance.uploadButton.interactable = true;
            Game_Load.Instance.PreventGameLoad();
        }
        
    }
    private void CreateTemporaryDirectory()
    {
        CreateFolder(_tempSaveDataPath+"/Player");
        CreateFolder(_tempSaveDataPath+"/Items");
        CreateFolder(_tempSaveDataPath+"/Items/Held_Items");
        CreateFolder(_tempSaveDataPath+"/Pokemon");
        CreateFolder(_tempSaveDataPath+"/Party_Ids");
    }
    public void CreateDefaultWebglDirectories()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
                        CreateDirectories();
        #endif
    }
    public void UploadSaveZip()
    {
        CreateDefaultWebglDirectories();
        #if UNITY_WEBGL && !UNITY_EDITOR
                                UploadZipAndStoreToIDBFS();
        #endif
    }
    public void OnFileStructureCreated()//js notification
    {
        //do something if want, in future
    }
    public void OnDownloadComplete()//js notification
    {
        Dialogue_handler.Instance.DisplayDetails("Save data downloaded successfully!");
    }
    public void OnIDBFSReady()//js notification
    {
        StartCoroutine(SyncFromIndexedDB());
    }
    private IEnumerator SyncFromIndexedDB()
    {
        Dialogue_handler.Instance.DisplayDetails("Game Loaded");
        LoadPlayerData(); 
        LoadItemData();
        LoadPokemonData();
        LoadOverworldData();
        yield return new WaitForSeconds(1f);
        Game_Load.Instance.AllowGameLoad();
    }
    string GetSavePath()
    {
        string basePath;

        switch (Application.platform)
        {
            case RuntimePlatform.WebGLPlayer:
                basePath = "/data/Save_data"; // root of virtual FS (in-memory or IDBFS)
                break;
             default:
                basePath = "Assets/Save_data";
                break;
        }
        return basePath;
    }

    private void LoadOverworldData()
    {
        CreateFolder(_saveDataPath + "/Overworld");
        CreateFolder(_saveDataPath + "/Overworld/Berry_Trees");
        var overworldTrees = GetJsonFilesFromPath(_saveDataPath + "/Overworld/Berry_Trees");
        foreach (var treeFilename in overworldTrees)
        {
            var jsonFilePath = _saveDataPath + "/Overworld/Berry_Trees/" + Path.GetFileName(treeFilename);
            if (!File.Exists(jsonFilePath)) continue;
            
            var json = File.ReadAllText(jsonFilePath);
            var treeData = ScriptableObject.CreateInstance<BerryTreeData>();
            JsonUtility.FromJsonOverwrite(json, treeData);
            treeData.loadedFromJson = true;
            treeData.berryItem = Resources.Load<Item>(GetDirectory(AssetDirectory.Berries)
                                                      + treeData.itemAssetName);
            OverworldState.Instance.LoadBerryTreeData(treeData);
        }

        OnOverworldDataLoaded?.Invoke();
    }
    private void LoadPlayerData()
    {
        CreateFolder(_saveDataPath + "/Player");
        Game_Load.Instance.playerData = null;
        var playerList = GetJsonFilesFromPath(_saveDataPath + "/Player");
        if(playerList.Count==1)
            Game_Load.Instance.playerData = LoadPlayerFromJson(_saveDataPath+"/Player/" + Path.GetFileName(playerList[0]));
        else if (playerList.Count > 1)
        {
            Dialogue_handler.Instance.DisplayDetails("Please ensure only one player's data is in the save_data folder!");
            Game_Load.Instance.PreventGameLoad();
        }
        else
        {
            Dialogue_handler.Instance.DisplayDetails("There was no save data found!");
            Dialogue_handler.Instance.canExitDialogue = false;
            Game_Load.Instance.PreventGameLoad();
        }
    }
    private void LoadItemData()
    {
        CreateFolder(_saveDataPath+"/Items");
        CreateFolder(_saveDataPath+"/Items/Held_Items");
        var itemList = GetJsonFilesFromPath(_saveDataPath+"/Items");
        Bag.Instance.bagItems.Clear();
        foreach(var item in itemList)
            Bag.Instance.bagItems.Add(LoadItemFromJson(_saveDataPath+"/Items/" + Path.GetFileName(item)));
    }
    private List<string> GetJsonFilesFromPath(string path)
    {
        List<string> jsonFiles=new();
        var files = Directory.GetFiles(path);
        foreach(var file in files)
            if (GetFileExtension(file) == ".json")
                jsonFiles.Add(file);
        return jsonFiles;
    }
    private string GetFileExtension(string filename)
    {
        return Path.GetExtension(filename);
    }
    string RemoveFileExtension(string filename)
    {
        return filename.Split('.')[0];
    }
    private void LoadPokemonData()
    {
        CreateFolder(_saveDataPath+"");
        CreateFolder(_saveDataPath+"/Pokemon");
        CreateFolder(_saveDataPath+"/Party_Ids");
        partyIDs.Clear();
        GetPartyPokemonIDs();
        pokemon_storage.Instance.numNonPartyPokemon = 0;
        pokemon_storage.Instance.totalPokemonCount = 0;
        Pokemon_party.Instance.numMembers = 0;
        for (int i = 0; i < pokemon_storage.Instance.numPartyMembers; i++)
        {
            var pokemon = LoadPokemonFromJson(_saveDataPath+"/Pokemon/" + partyIDs[i] + ".json");
            LoadHeldItems(pokemon);
            Pokemon_party.Instance.party[i] = pokemon;
            Pokemon_party.Instance.numMembers++;
        }
        pokemon_storage.Instance.nonPartyPokemon.Clear();
        var pokemonList = GetJsonFilesFromPath(_saveDataPath+"/Pokemon/");
        foreach(var file in pokemonList)
        {
            var fileName = Path.GetFileName(file);//filename is the pokemon id
            if (!pokemon_storage.Instance.IsPartyPokemon(RemoveFileExtension(fileName)))
            {
                var nonPartyPokemon = LoadPokemonFromJson(_saveDataPath+"/Pokemon/" + fileName);
                LoadHeldItems(nonPartyPokemon);
                pokemon_storage.Instance.nonPartyPokemon.Add(nonPartyPokemon);
                pokemon_storage.Instance.numNonPartyPokemon++;
            }
            pokemon_storage.Instance.totalPokemonCount++;
        }
    }

    private void LoadHeldItems(Pokemon pokemon)
    {
        if (!pokemon.hasItem) return;
        var heldItemList = GetJsonFilesFromPath(_saveDataPath+"/Items/Held_Items");
        if (heldItemList.Count > 0)
        {
            var heldItemID = heldItemList
                .FirstOrDefault(id => RemoveFileExtension(Path.GetFileName(id)) 
                                      == pokemon.pokemonID.ToString());
            
            var heldItem = (string.IsNullOrEmpty(heldItemID)) ? null : LoadItemFromJson(heldItemID);
            pokemon.GiveItem(heldItem);
        }
    }
    private void GetPartyPokemonIDs()
    {
        pokemon_storage.Instance.numPartyMembers = 0;
        var numIds = 0;
        var files = Directory.GetFiles(_saveDataPath+"/Party_Ids/");
        foreach(var file in files)
            if (GetFileExtension(file) == ".txt")
                numIds++;
        for (int i = 0; i < numIds; i++)
        {
            var currentID = File.ReadAllText(_saveDataPath+"/Party_Ids/" + "pkm_" + (i + 1) + ".txt");
            partyIDs.Add(currentID);
            pokemon_storage.Instance.numPartyMembers++;
        }
    }
    private void CreateFolder(string path)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer) return;
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
    private void ClearDirectory(string path)
    {
        var files = Directory.GetFiles(path);
        foreach (var file in files)
            File.Delete(file);
    }
    private IEnumerator CopyCorrectSaveData(string sourceDir, string destinationDir, bool recursive)
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
                yield return CopyCorrectSaveData(directory, destSubDir, true);
            }
        }

    }

    public void EraseSaveData()
    {
        ClearDirectory(_saveDataPath+"/Pokemon");
        ClearDirectory(_saveDataPath+"/Items");
        ClearDirectory(_saveDataPath+"/Player");
        ClearDirectory(_saveDataPath+"/Party_Ids");
    }
    private void EraseTemporarySaveData()
    {
        ClearDirectory(_tempSaveDataPath+"/Pokemon");
        ClearDirectory(_tempSaveDataPath+"/Items");
        ClearDirectory(_tempSaveDataPath+"/Player");
        ClearDirectory(_tempSaveDataPath+"/Party_Ids");
    }
    void SaveAllPokemonData(Pokemon pokemon)
    {
        pokemon.SaveUnserializableData();
        if(pokemon.hasItem)
            SaveHeldItem(pokemon.heldItem, pokemon.pokemonID.ToString());
        SavePokemonDataAsJson(pokemon);
    }

    void HandleSaveError(string errorMessage, Exception exception)
    {
        EraseTemporarySaveData();
        Debug.LogError(errorMessage+exception);
        Dialogue_handler.Instance.DisplayDetails("Error occured while saving please restart the game!");
        Dialogue_handler.Instance.EndDialogue(2f);
    }
    public IEnumerator SaveAllData()
    {
        Dialogue_handler.Instance.DisplayDetails("Saving...");
        
        for (int i = 0; i < pokemon_storage.Instance.numPartyMembers; i++)
        {
            try
            {
                SaveAllPokemonData(Pokemon_party.Instance.party[i]);
            }
            catch (Exception e)
            {
                OnSaveDataFail?.Invoke("Error occured with SaveAllPokemonData, exception: ",e);
                yield break;
            }
        }
        for (int i = 0; i < pokemon_storage.Instance.numNonPartyPokemon; i++)
        {
            try
            {
                SaveAllPokemonData(pokemon_storage.Instance.nonPartyPokemon[i]);
            }
            catch (Exception e)
            {
                OnSaveDataFail?.Invoke("Error occured with SaveNonPartyPokemonData, exception: ",e);
                yield break;
            }
        }

        try
        {
            SavePartyPokemonIDs();            
        }
        catch (Exception e)
        {
            OnSaveDataFail?.Invoke("Error occured with SavePartyPokemonIDs, exception: ",e);
            yield break;
        }
        
        foreach (var item in Bag.Instance.bagItems)
        {
            try
            {
                SaveItemDataAsJson(item, item.itemID);
            }
            catch (Exception e)
            {
                OnSaveDataFail?.Invoke("Error occured with SaveItemDataAsJson, exception: ",e);
                yield break;
            }
        }
        
        Game_Load.Instance.playerData.playerPosition = Player_movement.Instance.playerObject.transform.position;
        
        Game_Load.Instance.playerData.location = Game_Load.Instance.playerData.location==string.Empty? 
            "Overworld" 
            : area.currentArea.areaName;
        
        try
        {
            SavePlayerDataAsJson(Game_Load.Instance.playerData,Game_Load.Instance.playerData.trainerID.ToString());
        }
        catch (Exception e)
        {
            OnSaveDataFail?.Invoke("Error occured with SavePlayerDataAsJson, exception: ",e);
            yield break;
        }
        //save overworld Data
        yield return OnPlayerDataSaved?.Invoke();
        
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadZipAndStoreLocally();
#endif
            Dialogue_handler.Instance.DisplayDetails("Game saved online but please download your save file");
        }
        else
        {
            EraseSaveData();//empty old save data
            yield return new WaitForSeconds(1f);
            //copy new save data
            yield return StartCoroutine(CopyCorrectSaveData(_tempSaveDataPath
                            ,_saveDataPath,recursive: true));
            EraseTemporarySaveData();
            Dialogue_handler.Instance.DisplayDetails("Game saved");
            Dialogue_handler.Instance.EndDialogue(1f);
        }
    }

    private void SavePartyPokemonIDs()
    {
        for (int i = 0; i < pokemon_storage.Instance.numPartyMembers; i++)
        {
            if (Pokemon_party.Instance.party[i]==null) throw new Exception("party member is null! ");
            
            var path = Path.Combine(_tempSaveDataPath + "/Party_Ids/", "pkm_" + (i + 1) + ".txt");
            File.WriteAllText(path, Pokemon_party.Instance.party[i].pokemonID.ToString());
        }
    }

    private string DetermineImageDirectory(Item item)
    {
        if (item.additionalInfoModules.Any(m => m is TM))
        {
            var tm =  item.GetModule<TM>();
            return tm.move.type.typeName.ToLower() + " tm"; 
        }
        if (item.additionalInfoModules.Any(m => m is HM))
        {
            var hm = item.GetModule<HM>();
            return hm.move.type.typeName.ToLower() + " tm";     
        }
        
        return item.itemName;
    }
    private void SaveHeldItem(Item item, string fileName)
    {
        if(item==null) throw new Exception("Item is null! "+fileName); 
        
        var directory = Path.Combine(_tempSaveDataPath+"/Items/Held_Items", fileName + ".json");
        var json = JsonUtility.ToJson(item, true);
        File.WriteAllText(directory, json);
    }
    private void SavePokemonDataAsJson(Pokemon pokemon)
    {
        if(pokemon==null) throw new Exception("pokemon is null! ");
        
        var directory = Path.Combine(_tempSaveDataPath+"/Pokemon/", pokemon.pokemonID + ".json");
        var json = JsonUtility.ToJson(pokemon, true);
        File.WriteAllText(directory, json);
    }
    private void SavePlayerDataAsJson(Player_data player, string fileName)
    {
        if(player==null) throw new Exception("player data is null! ");
        
        var directory = Path.Combine(_tempSaveDataPath+"/Player", fileName + ".json");
        var json = JsonUtility.ToJson(player, true);
        File.WriteAllText(directory, json);
    }
    private void SaveItemDataAsJson(Item item, string fileName)
    {
        if(item==null) throw new Exception("Item is null! "+fileName); 
        
        var directory = Path.Combine(_tempSaveDataPath+"/Items", fileName + ".json");
        if(item.hasModules)
        {
            item.infoModuleAssetNames.Clear();
            if (item.additionalInfoModules.Count == 0 && !item.isMultiModular)
            {
                //just in-case
                item.additionalInfoModules.Add(item.additionalInfoModule);
            }
            foreach (var module in item.additionalInfoModules)
            {
                item.infoModuleAssetNames.Add(module.name);
            }
        }
        item.imageDirectory = DetermineImageDirectory(item);
        var json = JsonUtility.ToJson(item, true);
        File.WriteAllText(directory, json);
    }
    public void SaveBerryTreeDataAsJson(BerryTreeData tree, string fileName)
    {
        var directory = Path.Combine(_saveDataPath+"/Overworld/Berry_Trees", fileName + ".json");
        tree.itemAssetName = tree.berryItem.itemName;
        var json = JsonUtility.ToJson(tree, true);
        File.WriteAllText(directory, json);
    }
    private Pokemon LoadPokemonFromJson(string filePath)
    {
        if (!File.Exists(filePath))return null;
        var json = File.ReadAllText(filePath);
        var pokemon = ScriptableObject.CreateInstance<Pokemon>();
        JsonUtility.FromJsonOverwrite(json, pokemon);
        pokemon.LoadUnserializedData();
        return pokemon;
    }
    private Item LoadItemFromJson(string filePath)
    {
        if (!File.Exists(filePath))return null;
        var json = File.ReadAllText(filePath);
        var item = ScriptableObject.CreateInstance<Item>();
        JsonUtility.FromJsonOverwrite(json, item);
        if (item.hasModules)
        {
            item.additionalInfoModules.Clear();
            foreach (var assetName in item.infoModuleAssetNames)
            {
                var additionalInfo = Resources.Load<AdditionalInfoModule>(GetDirectory(AssetDirectory.AdditionalInfo)+assetName);
                item.additionalInfoModules.Add(additionalInfo);
            }
            item.additionalInfoModule = item.additionalInfoModules.First();
        }
        item.itemImage = Testing.CheckImage(GetDirectory(AssetDirectory.UI),item.imageDirectory);
        return item;
    }
    private Player_data LoadPlayerFromJson(string filePath)
    {
        if (!File.Exists(filePath))return null;
        var json = File.ReadAllText(filePath);
        var player = ScriptableObject.CreateInstance<Player_data>();
        JsonUtility.FromJsonOverwrite(json, player);
        return player;
    }
}
