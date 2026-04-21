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
}
public enum SaveDataDirectory
{
    Items, HeldItems, StorageItems, Pokemon, Player,
    PartyIds, PCStorage, Overworld, StoryObjectives, BerryTrees,
    GameSettings
}
public class Save_manager : MonoBehaviour,IInjectable
{
    [DllImport("__Internal")] private static extern void DownloadZipAndStoreLocally();
    [DllImport("__Internal")] private static extern void CreateDirectories(string jsonPtr);
    [DllImport("__Internal")] private static extern void UploadZipAndStoreToIDBFS();

    
    [SerializeField]private List<string> partyIDs;

    private string _saveDataPath;
    private string _tempSaveDataPath;
    private static string _rootAssetDirectory;
    private event Action<string,Exception> OnSaveDataFail;
    public event Action OnUploadedDataReady;
    public event Action OnVirtualFsCreated;
    private bool _virtualFileStructureReady;
    
    private Dialogue_handler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private Area_manager  _areaHandler;
    private pokemon_storage _pokemonStorageHandler;
    private Game_Load _gameLoadingHandler;
    private Pokemon_party _pokemonPartyHandler;
    private Player_movement _playerMovementHandler;
    private OverworldState _overworldStateHandler;
    private Bag _playerBagHandler;
    private GameSettingsHandler _gameSettingsHandler;
    private ServiceContainer _container;
    
    private static readonly Dictionary<AssetDirectory, string> AssetDirectories = new()
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
        {AssetDirectory.NonMartItems,"Items/NonMartItems/" },
        {AssetDirectory.MartItems,"Items/Mart_Items/" },
        {AssetDirectory.Items,"Items/" },
        {AssetDirectory.Berries,"Items/Berries/" },
        {AssetDirectory.AdditionalInfo,"Items/AdditionalInfo/" },
        {AssetDirectory.BerryTreeData,"Overwolrd_obj/Interactions/Berry Trees/Berry Data/"},
        {AssetDirectory.StoryObjectiveData,"Overwolrd_obj/Story Objectives/"},
        {AssetDirectory.PokeMartData,"Overwolrd_obj/Poke_Mart_Data"},
        {AssetDirectory.TrainerData,"Enemies/Data/"}
    };
    private static readonly Dictionary<SaveDataDirectory, string> SaveDataDirectories = new()
    {
        { SaveDataDirectory.Items, "/Items" },
        { SaveDataDirectory.HeldItems, "/Items/Held_Items" },
        { SaveDataDirectory.StorageItems, "/Items/Storage_Items" },
        { SaveDataDirectory.Pokemon, "/Pokemon" },
        { SaveDataDirectory.Player, "/Player" },
        { SaveDataDirectory.PartyIds, "/Party_Ids" },
        { SaveDataDirectory.PCStorage, "/PC_Storage" },
        { SaveDataDirectory.Overworld, "/Overworld" },
        { SaveDataDirectory.StoryObjectives, "/Overworld/Story_Objectives" },
        { SaveDataDirectory.BerryTrees, "/Overworld/Berry_Trees" },
        { SaveDataDirectory.GameSettings,"/GameSettings"}
    };

    public static string GetDirectory(AssetDirectory directoryKey)
    {
        return _rootAssetDirectory + AssetDirectories[directoryKey];
    }
    private string GetSaveDirectory(SaveDataDirectory directoryKey)
    {
        return SaveDataDirectories[directoryKey]+"/";
    }
    public void Inject(ServiceContainer container)
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
        _gameSettingsHandler = container.Resolve<GameSettingsHandler>();
        _container = container;
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
    {
        OnSaveDataFail += HandleSaveError;
        _virtualFileStructureReady = false;
        OnVirtualFsCreated += () => _virtualFileStructureReady = true;
        
        _rootAssetDirectory = "Pokemon_project_assets/";
        
        switch (Application.platform)
        {
            case RuntimePlatform.WebGLPlayer:
                _saveDataPath = "/data/Save_data";
                _tempSaveDataPath = "/data/Temp_Save_data";
                break;
            default:
                _saveDataPath = "Assets/Save_data";
                _tempSaveDataPath="Assets/Temp_Save_data";
                break;
        }
        
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
        CreateFolder(_tempSaveDataPath + GetSaveDirectory(SaveDataDirectory.Player));
        CreateFolder(_tempSaveDataPath + GetSaveDirectory(SaveDataDirectory.Items));
        CreateFolder(_tempSaveDataPath + GetSaveDirectory(SaveDataDirectory.HeldItems));
        CreateFolder(_tempSaveDataPath + GetSaveDirectory(SaveDataDirectory.StorageItems));
        CreateFolder(_tempSaveDataPath + GetSaveDirectory(SaveDataDirectory.Pokemon));
        CreateFolder(_tempSaveDataPath + GetSaveDirectory(SaveDataDirectory.PartyIds));
        CreateFolder(_tempSaveDataPath + GetSaveDirectory(SaveDataDirectory.PCStorage));
        CreateFolder(_tempSaveDataPath + GetSaveDirectory(SaveDataDirectory.StoryObjectives));
    }

    [Serializable]
    private class StringArrayWrapper
    {
        public string[] items;
    }
    public void CreateDefaultWebglDirectories()
    {
        List<string> directoryList = new();
        foreach (var dir in SaveDataDirectories)
        {
            directoryList.Add(dir.Value);
        }
        var wrapper = new StringArrayWrapper
        {
            items = directoryList.ToArray()
        };

        string json = JsonUtility.ToJson(wrapper);
       
        if (Application.platform != RuntimePlatform.WebGLPlayer) return;
        CreateDirectories(json);
    }
    public void UploadSaveZip()
    {
        StartCoroutine(ProcessFileUpload());
    }
    private IEnumerator ProcessFileUpload()
    {
        CreateDefaultWebglDirectories();
        yield return new WaitUntil(() => _virtualFileStructureReady);
        UploadZipAndStoreToIDBFS();
    }
    
    public void OnFileStructureCreated()//js notification
    {
        OnVirtualFsCreated?.Invoke();
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
        OnUploadedDataReady?.Invoke();
        LoadPlayerData(); 
        LoadItemData();
        LoadPokemonData();
        yield return new WaitForSecondsRealtime(1f);
        _gameLoadingHandler.AllowGameLoad();
    }
    public List<SettingsConfig> LoadGameSettingsData()
    {
        CreateFolder(_saveDataPath + GetSaveDirectory(SaveDataDirectory.GameSettings));
        var jsonFilesFromPath = GetJsonFilesFromPath(_saveDataPath + GetSaveDirectory(SaveDataDirectory.GameSettings));
        List<SettingsConfig> savedSettingConfigs = new();  
        foreach (var fullPath in jsonFilesFromPath)
        {
            var jsonFilePath = _saveDataPath + GetSaveDirectory(SaveDataDirectory.GameSettings) + Path.GetFileName(fullPath);
            if (!File.Exists(jsonFilePath)) continue;
              
            var json = File.ReadAllText(jsonFilePath);
            var configData = new SettingsConfig();
            JsonUtility.FromJsonOverwrite(json, configData);
            savedSettingConfigs.Add(configData);
        }
        return savedSettingConfigs;
    }
    public List<PokemonStorageBox> LoadPokemonStorageData()
    {
        CreateFolder(_saveDataPath + GetSaveDirectory(SaveDataDirectory.PCStorage));
        var storageBoxes = GetJsonFilesFromPath(_saveDataPath + GetSaveDirectory(SaveDataDirectory.PCStorage));
        List<PokemonStorageBox> savedStorageBoxes = new(); 
        foreach (var boxFullPath in storageBoxes)
        {  
            var jsonFilename = _saveDataPath + GetSaveDirectory(SaveDataDirectory.PCStorage) + Path.GetFileName(boxFullPath);
            if (!File.Exists(jsonFilename)) continue;
            
            var json = File.ReadAllText(jsonFilename);
            var boxData = ScriptableObject.CreateInstance<PokemonStorageBox>();
            JsonUtility.FromJsonOverwrite(json, boxData);
            savedStorageBoxes.Add(boxData);
        }

        return savedStorageBoxes;
    }
    public void LoadOverworldData()
    {
        CreateFolder(_saveDataPath + GetSaveDirectory(SaveDataDirectory.Overworld));
        CreateFolder(_saveDataPath + GetSaveDirectory(SaveDataDirectory.BerryTrees));
        var overworldTrees = GetJsonFilesFromPath(_saveDataPath + GetSaveDirectory(SaveDataDirectory.BerryTrees));
        foreach (var jsonFilePath in overworldTrees)
        {
            var treeFilename = _saveDataPath + GetSaveDirectory(SaveDataDirectory.BerryTrees) + Path.GetFileName(jsonFilePath);
            if (!File.Exists(treeFilename)) continue;
            
            var json = File.ReadAllText(treeFilename);
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
        CreateFolder(_saveDataPath + GetSaveDirectory(SaveDataDirectory.StoryObjectives));
        var storyObjectives = GetJsonFilesFromPath(_saveDataPath + GetSaveDirectory(SaveDataDirectory.StoryObjectives));
        foreach (var jsonFilePath in storyObjectives)
        {
            var objective = _saveDataPath + GetSaveDirectory(SaveDataDirectory.StoryObjectives) + Path.GetFileName(jsonFilePath);
            if (!File.Exists(objective)) continue;
 
            var rawJson = File.ReadAllText(objective);
            var wrapper = JsonUtility.FromJson<ObjectiveTypeWrapper>(rawJson);
            var objectiveData = StoryObjective.CreateObjectiveOfType(wrapper.objectiveType);
            JsonUtility.FromJsonOverwrite(rawJson, objectiveData);
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
        CreateFolder(_saveDataPath + GetSaveDirectory(SaveDataDirectory.Player));
        _gameLoadingHandler.playerData = null;
        var playerList = GetJsonFilesFromPath(_saveDataPath + GetSaveDirectory(SaveDataDirectory.Player));
        if(playerList.Count==1)
            _gameLoadingHandler.playerData = LoadPlayerFromJson(_saveDataPath+ GetSaveDirectory(SaveDataDirectory.Player) + Path.GetFileName(playerList[0]));
        else if (playerList.Count > 1)
        {
            _dialogueHandler.DisplayDetails("Please ensure only one player's data is in the save_data folder!");
            _gameLoadingHandler.PreventGameLoad();
        }
        else
        {
            _gameLoadingHandler.PreventGameLoad();
        }
    }
    private void LoadItemData()
    {
        CreateFolder(_saveDataPath+GetSaveDirectory(SaveDataDirectory.Items));
        CreateFolder(_saveDataPath+GetSaveDirectory(SaveDataDirectory.HeldItems));
        CreateFolder(_saveDataPath+GetSaveDirectory(SaveDataDirectory.StorageItems));  
        
        var itemList = GetJsonFilesFromPath(_saveDataPath+GetSaveDirectory(SaveDataDirectory.Items));
        var storageItemList = GetJsonFilesFromPath(_saveDataPath+GetSaveDirectory(SaveDataDirectory.StorageItems));
        _playerBagHandler.allItems.Clear();
        foreach (var item in itemList)
        {
            _playerBagHandler.allItems.Add(LoadItemFromJson(_saveDataPath + GetSaveDirectory(SaveDataDirectory.Items) + Path.GetFileName(item)));
        }
        foreach (var item in storageItemList)
        {
            _playerBagHandler.storageItems.Add(LoadItemFromJson(_saveDataPath + GetSaveDirectory(SaveDataDirectory.StorageItems) + Path.GetFileName(item)));
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
        CreateFolder(_saveDataPath);
        CreateFolder(_saveDataPath+GetSaveDirectory(SaveDataDirectory.Pokemon));
        CreateFolder(_saveDataPath+GetSaveDirectory(SaveDataDirectory.PartyIds));
        partyIDs.Clear();
        GetPartyPokemonIDs();
        _pokemonStorageHandler.numNonPartyPokemon = 0;
        _pokemonStorageHandler.totalPokemonCount = 0;
        _pokemonPartyHandler.numMembers = 0;
        for (int i = 0; i < _pokemonStorageHandler.numPartyMembers; i++)
        {
            var pokemon = LoadPokemonFromJson(_saveDataPath + GetSaveDirectory(SaveDataDirectory.Pokemon) + partyIDs[i] + ".json");
            LoadHeldItems(pokemon);
            _pokemonPartyHandler.party[i] = pokemon;
            _pokemonPartyHandler.numMembers++;
        }
        _pokemonStorageHandler.nonPartyPokemon.Clear();
        var pokemonList = GetJsonFilesFromPath(_saveDataPath + GetSaveDirectory(SaveDataDirectory.Pokemon));
        foreach(var file in pokemonList)
        {
            var fileName = Path.GetFileName(file);//filename is the pokemon id
            var pokemonID = RemoveFileExtension(fileName);
            var isPartyPokemon = partyIDs.Any(id => id == pokemonID);
            if (!isPartyPokemon)
            {
                var nonPartyPokemon = LoadPokemonFromJson(_saveDataPath+ GetSaveDirectory(SaveDataDirectory.Pokemon) + fileName);
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
        var heldItemList = GetJsonFilesFromPath(_saveDataPath+GetSaveDirectory(SaveDataDirectory.HeldItems));
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
        var files = Directory.GetFiles( _saveDataPath + GetSaveDirectory(SaveDataDirectory.PartyIds));
        foreach(var file in files)
            if (GetFileExtension(file) == ".txt")
                numIds++;
        for (int i = 0; i < numIds; i++)
        {
            var currentID = File.ReadAllText(_saveDataPath + GetSaveDirectory(SaveDataDirectory.PartyIds) + "pkm_" + (i + 1) + ".txt");
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
        foreach (var dir in SaveDataDirectories)
        {
            ClearDirectory(_saveDataPath + dir.Value);
        }
    }
    private void EraseTemporarySaveData()
    {
        foreach (var dir in SaveDataDirectories)
        {
            ClearDirectory(_tempSaveDataPath + dir.Value);
        }
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
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            _virtualFileStructureReady = false;
            CreateDefaultWebglDirectories();
            yield return new WaitUntil(() => _virtualFileStructureReady);
        }
        
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
        
        yield return _gameSettingsHandler.SaveSettings();
        
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            DownloadZipAndStoreLocally();
            _dialogueHandler.DisplayDetails("Game saved online but please download your save file");
        }
        else
        {
            EraseSaveData();//empty old save data
            yield return new WaitForSecondsRealtime(1f);
            //copy new save data
            yield return CopyCorrectSaveData(_tempSaveDataPath,_saveDataPath,recursive: true);
            yield return new WaitForSecondsRealtime(1f);
            EraseTemporarySaveData();
            _dialogueHandler.DisplayDetails("Game saved",false);
        }
        _dialogueHandler.EndDialogue(1.5f);
        yield return new WaitForSecondsRealtime(1.4f);
        _inputStateHandler.ResetRelevantUi(InputStateName.DialoguePlaceHolder,true);
    }

    private void SavePartyPokemonIDs()
    {
        for (int i = 0; i < _pokemonStorageHandler.numPartyMembers; i++)
        {
            if (_pokemonPartyHandler.party[i]==null) throw new Exception("party member is null! ");
            
            var path = Path.Combine(_tempSaveDataPath + GetSaveDirectory(SaveDataDirectory.PartyIds), "pkm_" + (i + 1) + ".txt");
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
        
        var directory = Path.Combine(_tempSaveDataPath+GetSaveDirectory(SaveDataDirectory.HeldItems), fileName + ".json");
        var json = JsonUtility.ToJson(item, true);
        File.WriteAllText(directory, json);
    }
    private void SaveStorageItem(Item item, string fileName)
    {
        if(item==null) throw new Exception("Item is null! "+fileName); 
        
        var directory = Path.Combine(_tempSaveDataPath+GetSaveDirectory(SaveDataDirectory.StorageItems), fileName + ".json");
        var json = JsonUtility.ToJson(item, true);
        File.WriteAllText(directory, json);
    }
    private void SavePokemonDataAsJson(Pokemon pokemon)
    {
        if(pokemon==null) throw new Exception("pokemon is null! ");
        
        var directory = Path.Combine(_tempSaveDataPath + GetSaveDirectory(SaveDataDirectory.Pokemon), pokemon.pokemonID + ".json");
        var json = JsonUtility.ToJson(pokemon, true);
        File.WriteAllText(directory, json);
    }
    private void SavePlayerDataAsJson(Player_data player, string fileName)
    {
        if(player==null) throw new Exception("player data is null! ");
        
        var directory = Path.Combine(_tempSaveDataPath+GetSaveDirectory(SaveDataDirectory.Player), fileName + ".json");
        var json = JsonUtility.ToJson(player, true);
        File.WriteAllText(directory, json);
    }
    private void SaveItemDataAsJson(Item item, string fileName)
    {
        if(item==null) throw new Exception("Item is null! "+fileName); 
        
        var directory = Path.Combine(_tempSaveDataPath+GetSaveDirectory(SaveDataDirectory.Items), fileName + ".json");
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
        var directory = Path.Combine(_saveDataPath+GetSaveDirectory(SaveDataDirectory.BerryTrees), fileName + ".json");
        tree.itemAssetName = tree.berryItem.itemName;
        var json = JsonUtility.ToJson(tree, true);
        File.WriteAllText(directory, json);
    }
    public void SaveStoryDataAsJson(StoryObjective objective, string fileName)
    {
        var directory = Path.Combine(_tempSaveDataPath+GetSaveDirectory(SaveDataDirectory.StoryObjectives), fileName + ".json");
        var json = JsonUtility.ToJson(objective, true);
        File.WriteAllText(directory, json);
    }
    public void SaveStorageDataAsJson(PokemonStorageBox box, string fileName)
    {
        var directory = Path.Combine(_saveDataPath+GetSaveDirectory(SaveDataDirectory.PCStorage), fileName + ".json");
        var json = JsonUtility.ToJson(box, true);
        
        File.WriteAllText(directory, json);
        if (!File.Exists(directory)) Debug.LogError("file blank");
    }
    public void SaveGameSettingsAsJson(SettingsConfig config, string fileName)
    {
        var directory = Path.Combine(_saveDataPath + GetSaveDirectory(SaveDataDirectory.GameSettings), fileName + ".json");
        var json = JsonUtility.ToJson(config, true);
        
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


