using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Serialization;

public enum AssetDirectory
{ 
    Status, Moves, Abilities, Types, Natures, Pokemon, PokemonImage, UI, Items, MartItems, NonMartItems
    ,AdditionalInfo,Berries,BerryTreeData,PokeMartData,TrainerData,PokemonPartyImage,StoryObjectiveData
};
public class Save_manager : MonoBehaviour,IInjectable
{
    [DllImport("__Internal")] private static extern void DownloadZipAndStoreLocally();
    [DllImport("__Internal")] private static extern void CreateDirectories();
    [DllImport("__Internal")] private static extern void UploadZipAndStoreToIDBFS();
    [FormerlySerializedAs("party_IDs")] public List<string> partyIDs;
    public static Save_manager Instance { get; private set; }
    private string _saveDataPath = "Assets/Save_data";
    private string _tempSaveDataPath = "Assets/Temp_Save_data";
    private event Action<string,Exception> OnSaveDataFail;
    
    private Dialogue_handler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private Area_manager  _areaHandler;
    private pokemon_storage _pokemonStorageHandler;
    private Game_Load _gameLoadingHandler;
    private Pokemon_party _pokemonPartyHandler;
    private Player_movement _playerMovementHandler;
    private OverworldState _overworldStateHandler;
    private Bag _playerBagHandler;
    private Container _container;
    
    private static readonly Dictionary<AssetDirectory, string> Directories = new()
    {
        {AssetDirectory.Moves,"Pokemon_project_assets/Pokemon_obj/Moves/" },
        {AssetDirectory.Status,"Pokemon_project_assets/Pokemon_obj/Status/" },
        {AssetDirectory.Pokemon,"Pokemon_project_assets/Pokemon_obj/Pokemon/" },
        {AssetDirectory.PokemonImage,"Pokemon_project_assets/pokemon_img/" },
        {AssetDirectory.PokemonPartyImage,"Pokemon_project_assets/pokemon_img/party_img/"},
        {AssetDirectory.Abilities,"Pokemon_project_assets/Pokemon_obj/Abilities/" },
        {AssetDirectory.Types,"Pokemon_project_assets/Pokemon_obj/Types/" },
        {AssetDirectory.Natures,"Pokemon_project_assets/Pokemon_obj/Natures/" },
        {AssetDirectory.UI,"Pokemon_project_assets/UI/" },
        {AssetDirectory.NonMartItems,"Pokemon_project_assets/Items/NonMartItems/" },
        {AssetDirectory.MartItems,"Pokemon_project_assets/Items/Mart_Items/" },
        {AssetDirectory.Items,"Pokemon_project_assets/Items/" },
        {AssetDirectory.Berries,"Pokemon_project_assets/Items/Berries/" },
        {AssetDirectory.AdditionalInfo,"Pokemon_project_assets/Items/AdditionalInfo/" },
        {AssetDirectory.BerryTreeData,"Pokemon_project_assets/Overwolrd_obj/Interactions/Berry Trees/Berry Data/"},
        {AssetDirectory.StoryObjectiveData,"Pokemon_project_assets/Overwolrd_obj/Story Objectives/"},
        {AssetDirectory.PokeMartData,"Pokemon_project_assets/Overwolrd_obj/Poke_Mart_Data"},
        {AssetDirectory.TrainerData,"Pokemon_project_assets/Enemies/Data/"}
    };
    
    public static string GetDirectory(AssetDirectory directory)
    {
        return Directories[directory];
    }

    public void Inject(Container container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _pokemonStorageHandler = container.Resolve<pokemon_storage>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        _areaHandler = container.Resolve<Area_manager>();
        _overworldStateHandler = container.Resolve<OverworldState>();
        _playerBagHandler = container.Resolve<Bag>();
        _container = container;
        gameObject.SetActive(true);
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
            _gameLoadingHandler.uploadButton.interactable = false;
            LoadPlayerData(); 
            LoadItemData();
            LoadPokemonData();
        }
        else
        {
            _gameLoadingHandler.uploadButton.interactable = true;
            _gameLoadingHandler.PreventGameLoad();
        }
        
    }
    private void CreateTemporaryDirectory()
    {
        CreateFolder(_tempSaveDataPath+"/Player");
        CreateFolder(_tempSaveDataPath+"/Items");
        CreateFolder(_tempSaveDataPath+"/Items/Held_Items");
        CreateFolder(_tempSaveDataPath+"/Items/Storage_Items");
        CreateFolder(_tempSaveDataPath+"/Pokemon");
        CreateFolder(_tempSaveDataPath+"/Party_Ids");
        CreateFolder(_tempSaveDataPath + "/PC_Storage");
        CreateFolder(_tempSaveDataPath + "/Overworld/Story_Objectives");
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
        _dialogueHandler.DisplayDetails("Save data downloaded successfully!");
    }
    public void OnIDBFSReady()//js notification
    {
        StartCoroutine(SyncFromIndexedDB());
    }
    private IEnumerator SyncFromIndexedDB()
    {
        _dialogueHandler.DisplayDetails("Game Loaded");
        LoadPlayerData(); 
        LoadItemData();
        LoadPokemonData();
        yield return new WaitForSeconds(1f);
        _gameLoadingHandler.AllowGameLoad();
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
    public void LoadPokemonStorageData()
    {
        CreateFolder(_saveDataPath + "/PC_Storage");
        var storageBoxes = GetJsonFilesFromPath(_saveDataPath + "/PC_Storage");
        foreach (var boxJson in storageBoxes)
        {
            var jsonFilePath = _saveDataPath + "/PC_Storage/" + Path.GetFileName(boxJson);
            if (!File.Exists(jsonFilePath)) continue;
            
            var json = File.ReadAllText(jsonFilePath);
            var boxData = ScriptableObject.CreateInstance<PokemonStorageBox>();
            JsonUtility.FromJsonOverwrite(json, boxData);
            _pokemonStorageHandler.storageBoxes[boxData.boxNumber-1].boxPokemon = boxData.boxPokemon;
            _pokemonStorageHandler.storageBoxes[boxData.boxNumber-1].currentNumPokemon = boxData.currentNumPokemon;
        }
    }
    public void LoadOverworldData()
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
            treeData.spriteData.Clear();
            
            var treeSprites = Resources.Load<BerryTreeData>(
                GetDirectory(AssetDirectory.BerryTreeData) + $"{treeData.itemAssetName } Data").spriteData;
            treeData.spriteData = treeSprites;
            treeData.berryItem = Resources.Load<Item>(GetDirectory(AssetDirectory.Berries)
                                                      + treeData.itemAssetName);
            
            _overworldStateHandler.StoreBerryTreeData(treeData);
        }
        CreateFolder(_saveDataPath + "/Overworld/Story_Objectives");
        var storyObjectives = GetJsonFilesFromPath(_saveDataPath + "/Overworld/Story_Objectives");
        foreach (var objective in storyObjectives)
        {
            var jsonFilePath = _saveDataPath + "/Overworld/Story_Objectives/" + Path.GetFileName(objective);
            if (!File.Exists(jsonFilePath)) continue;

            var json = File.ReadAllText(jsonFilePath);
            var wrapper = JsonUtility.FromJson<ObjectiveTypeWrapper>(json);
            var objectiveData = StoryObjective.GetObjectiveType(wrapper.objectiveType);
            JsonUtility.FromJsonOverwrite(json, objectiveData);
            if (objectiveData is StoryProgressObjective storyData)
            {
                _overworldStateHandler.LoadStoryProgress(storyData);
            }
            else
            {
                _overworldStateHandler.currentStoryObjectives.Add(objectiveData);
            }
        }
    }
    private void LoadPlayerData()
    {
        CreateFolder(_saveDataPath + "/Player");
        _gameLoadingHandler.playerData = null;
        var playerList = GetJsonFilesFromPath(_saveDataPath + "/Player");
        if(playerList.Count==1)
            _gameLoadingHandler.playerData = LoadPlayerFromJson(_saveDataPath+"/Player/" + Path.GetFileName(playerList[0]));
        else if (playerList.Count > 1)
        {
            _dialogueHandler.DisplayDetails("Please ensure only one player's data is in the save_data folder!");
            _gameLoadingHandler.PreventGameLoad();
        }
        else
        {
            _dialogueHandler.DisplayDetails("There was no save data found!");
            _dialogueHandler.canExitDialogue = false;
            _gameLoadingHandler.PreventGameLoad();
        }
    }
    private void LoadItemData()
    {
        CreateFolder(_saveDataPath+"/Items");
        CreateFolder(_saveDataPath+"/Items/Held_Items");
        CreateFolder(_saveDataPath+"/Items/Storage_Items");  
        
        var itemList = GetJsonFilesFromPath(_saveDataPath+"/Items");
        var storageItemList = GetJsonFilesFromPath(_saveDataPath+"/Items/Storage_Items");
        _playerBagHandler.allItems.Clear();
        foreach (var item in itemList)
        {
            _playerBagHandler.allItems.Add(LoadItemFromJson(_saveDataPath+"/Items/" + Path.GetFileName(item)));
        }
        foreach (var item in storageItemList)
        {
            _playerBagHandler.storageItems.Add(LoadItemFromJson(_saveDataPath+"/Items/Storage_Items/" + Path.GetFileName(item)));
        }
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
        _pokemonStorageHandler.numNonPartyPokemon = 0;
        _pokemonStorageHandler.totalPokemonCount = 0;
        _pokemonPartyHandler.numMembers = 0;
        for (int i = 0; i < _pokemonStorageHandler.numPartyMembers; i++)
        {
            var pokemon = LoadPokemonFromJson(_saveDataPath+"/Pokemon/" + partyIDs[i] + ".json");
            LoadHeldItems(pokemon);
            _pokemonPartyHandler.party[i] = pokemon;
            _pokemonPartyHandler.numMembers++;
        }
        _pokemonStorageHandler.nonPartyPokemon.Clear();
        var pokemonList = GetJsonFilesFromPath(_saveDataPath+"/Pokemon/");
        foreach(var file in pokemonList)
        {
            var fileName = Path.GetFileName(file);//filename is the pokemon id
            if (!_pokemonStorageHandler.IsPartyPokemon(RemoveFileExtension(fileName)))
            {
                var nonPartyPokemon = LoadPokemonFromJson(_saveDataPath+"/Pokemon/" + fileName);
                LoadHeldItems(nonPartyPokemon);
                _pokemonStorageHandler.nonPartyPokemon.Add(nonPartyPokemon);
                _pokemonStorageHandler.numNonPartyPokemon++;
            }
            _pokemonStorageHandler.totalPokemonCount++;
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
        _pokemonStorageHandler.numPartyMembers = 0;
        var numIds = 0;
        var files = Directory.GetFiles(_saveDataPath+"/Party_Ids/");
        foreach(var file in files)
            if (GetFileExtension(file) == ".txt")
                numIds++;
        for (int i = 0; i < numIds; i++)
        {
            var currentID = File.ReadAllText(_saveDataPath+"/Party_Ids/" + "pkm_" + (i + 1) + ".txt");
            partyIDs.Add(currentID);
            _pokemonStorageHandler.numPartyMembers++;
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
        ClearDirectory(_saveDataPath + "/Overworld/Story_Objectives");
    }
    private void EraseTemporarySaveData()
    {
        ClearDirectory(_tempSaveDataPath+"/Pokemon");
        ClearDirectory(_tempSaveDataPath+"/Items");
        ClearDirectory(_tempSaveDataPath+"/Player");
        ClearDirectory(_tempSaveDataPath+"/Party_Ids");
        ClearDirectory(_tempSaveDataPath + "/Overworld/Story_Objectives");
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
        Debug.LogError(errorMessage+exception);
        _dialogueHandler.DisplayDetails("Error occured while saving please restart the game!");
        EraseTemporarySaveData();
        _inputStateHandler.ResetRelevantUi(InputStateName.DialoguePlaceHolder,true);
    }
    public IEnumerator SaveAllData()
    {
        _inputStateHandler.ResetRelevantUi(InputStateName.PlayerMenu);
        _inputStateHandler.AddDialoguePlaceHolderState();
        _dialogueHandler.DisplayDetails("Saving...",false); 
        for (int i = 0; i < _pokemonStorageHandler.numPartyMembers; i++)
        {
            try
            {
                SaveAllPokemonData(_pokemonPartyHandler.party[i]);
            }
            catch (Exception e)
            {
                OnSaveDataFail?.Invoke("Error occured with SaveAllPokemonData, exception: ",e);
                yield break;
            }
        }
        for (int i = 0; i < _pokemonStorageHandler.numNonPartyPokemon; i++)
        {
            try
            {
                SaveAllPokemonData(_pokemonStorageHandler.nonPartyPokemon[i]);
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
        
        foreach (var item in _playerBagHandler.allItems)
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
        foreach (var item in _playerBagHandler.storageItems)
        {
            try
            {
                SaveStorageItem(item, item.itemID);
            }
            catch (Exception e)
            {
                OnSaveDataFail?.Invoke("Error occured with SaveStorageItem, exception: ",e);
                yield break;
            }
        }
        _gameLoadingHandler.playerData.playerPosition = _playerMovementHandler.GetPlayerPosition();
        _gameLoadingHandler.playerData.location = _areaHandler.currentArea.data.areaName;
        
        try
        {
            SavePlayerDataAsJson(_gameLoadingHandler.playerData,_gameLoadingHandler.playerData.trainerID.ToString());
        }
        catch (Exception e)
        {
            OnSaveDataFail?.Invoke("Error occured with SavePlayerDataAsJson, exception: ",e);
            yield break;
        }

        yield return _overworldStateHandler.SaveOverworldData();
        
        yield return _pokemonStorageHandler.SaveStorageData();
        
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadZipAndStoreLocally();
#endif
            _dialogueHandler.DisplayDetails("Game saved online but please download your save file");
        }
        else
        {
            EraseSaveData();//empty old save data
            yield return new WaitForSeconds(1f);
            //copy new save data
            yield return CopyCorrectSaveData(_tempSaveDataPath,_saveDataPath,recursive: true);
            yield return new WaitForSeconds(1f);
            EraseTemporarySaveData();
            _dialogueHandler.DisplayDetails("Game saved",false);
        }
        _dialogueHandler.EndDialogue(1.5f);
        yield return new WaitForSeconds(1.4f);
        _inputStateHandler.ResetRelevantUi(InputStateName.DialoguePlaceHolder,true);
    }

    private void SavePartyPokemonIDs()
    {
        for (int i = 0; i < _pokemonStorageHandler.numPartyMembers; i++)
        {
            if (_pokemonPartyHandler.party[i]==null) throw new Exception("party member is null! ");
            
            var path = Path.Combine(_tempSaveDataPath + "/Party_Ids/", "pkm_" + (i + 1) + ".txt");
            File.WriteAllText(path, _pokemonPartyHandler.party[i].pokemonID.ToString());
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
    private void SaveStorageItem(Item item, string fileName)
    {
        if(item==null) throw new Exception("Item is null! "+fileName); 
        
        var directory = Path.Combine(_tempSaveDataPath+"/Items/Storage_Items", fileName + ".json");
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
    public void SaveStoryDataAsJson(StoryObjective objective, string fileName)
    {
        var directory = Path.Combine(_tempSaveDataPath+"/Overworld/Story_Objectives", fileName + ".json");
        var json = JsonUtility.ToJson(objective, true);
        File.WriteAllText(directory, json);
    }
    public void SaveStorageDataAsJson(PokemonStorageBox box, string fileName)
    {
        var directory = Path.Combine(_saveDataPath+"/PC_Storage", fileName + ".json");
        var json = JsonUtility.ToJson(box, true);
        
        File.WriteAllText(directory, json);
        if (!File.Exists(directory)) Debug.LogError("file blank");
    }
    private Pokemon LoadPokemonFromJson(string filePath)
    {
        if (!File.Exists(filePath))return null;
        var json = File.ReadAllText(filePath);
        var pokemon = ScriptableObject.CreateInstance<Pokemon>();
        JsonUtility.FromJsonOverwrite(json, pokemon);
        pokemon.LoadDataAndDependencies(_container);
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


