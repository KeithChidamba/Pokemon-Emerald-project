using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;


public enum AssetDirectory
{ 
    Status, Moves, Abilities, Types, Natures, Pokemon, PokemonImage, UI, Items, MartItems, NonMartItems
    ,AdditionalInfo,Berries,BerryTreeData,PokeMartData,TrainerData,PokemonPartyImage,StoryObjectiveData
}
public enum SaveDataDirectory
{
    Items, HeldItems, StorageItems, StoragePokemon, PartyPokemon, Player,
    PCStorage, Overworld, StoryObjectives, BerryTrees,
    GameSettings
}
public class SaveDataHandler : MonoBehaviour,IInjectable
{
    [DllImport("__Internal")] private static extern void DownloadZipAndStoreLocally();
    [DllImport("__Internal")] private static extern void CreateDirectories(string jsonPtr);
    [DllImport("__Internal")] private static extern void UploadZipAndStoreToIDBFS();
    [DllImport("__Internal")] private static extern void ClearFileDataStore();

    private string _saveDataPath;
    private string _tempSaveDataPath;
    private static string _rootAssetDirectory;
    private event Action<string,Exception> OnSaveDataFail;
    public event Action OnUploadedDataReady;
    public event Action OnVirtualFsCreated;
    private bool _virtualFileStructureReady;
    private bool _virtualDirectoriesCleared;
    
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
        { SaveDataDirectory.StoragePokemon, "/Storage_Pokemon" },
        { SaveDataDirectory.PartyPokemon, "/Party_Pokemon" },
        { SaveDataDirectory.Player, "/Player" },
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
        
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            CreateAllSaveDirectories();
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

    private void CreateAllSaveDirectories()
    {
        foreach (var dir in SaveDataDirectories)
        {
            if (!Directory.Exists(_tempSaveDataPath + dir.Value))
            {
                Directory.CreateDirectory(_tempSaveDataPath + dir.Value);
            }
            if (!Directory.Exists(_saveDataPath + dir.Value))
            {
                Directory.CreateDirectory(_saveDataPath + dir.Value);
            }
        }
    }

    [Serializable]
    private class StringArrayWrapper
    {
        public string[] items;
    }
    public IEnumerator CreateDefaultWebglDirectories()
    {
        ClearFileDataStore();
        _virtualDirectoriesCleared = false;
        yield return new WaitUntil(() => _virtualDirectoriesCleared);
        
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
        CreateDirectories(json);
    }
    
    public void UploadSaveZip()
    {
        StartCoroutine(ProcessFileUpload());
    }
    private IEnumerator ProcessFileUpload()
    {
        _virtualFileStructureReady = false;
        yield return CreateDefaultWebglDirectories();
        yield return new WaitUntil(() => _virtualFileStructureReady);
        UploadZipAndStoreToIDBFS();
    }
    public void OnFSCleared()//js notification
    {
        _virtualDirectoriesCleared = true;
    }
    public void OnFileStructureCreated()//js notification
    {
        _virtualFileStructureReady = true;
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
        var storageBoxes = GetJsonFilesFromPath(_saveDataPath + GetSaveDirectory(SaveDataDirectory.PCStorage));
        List<PokemonStorageBox> savedStorageBoxes = new(); 
        foreach (var boxFullPath in storageBoxes)
        {  
            var boxData = LoadObjectFromJson<PokemonStorageBox>(boxFullPath);
            savedStorageBoxes.Add(boxData);
        }
        return savedStorageBoxes;
    }
    public IEnumerator LoadOverworldData()
    {
        var overworldTrees = GetJsonFilesFromPath(_saveDataPath + GetSaveDirectory(SaveDataDirectory.BerryTrees));
        foreach (var jsonFilePath in overworldTrees)
        {
            var treeData = LoadObjectFromJson<BerryTreeData>(jsonFilePath);
            treeData.spriteData.Clear();
            var treeSprites = Resources.Load<BerryTreeData>(
                GetDirectory(AssetDirectory.BerryTreeData) + $"{treeData.itemAssetName } Data").spriteData;
            treeData.spriteData = treeSprites;
            treeData.berryItem = Resources.Load<Item>(GetDirectory(AssetDirectory.Berries)
                                                      + treeData.itemAssetName);
            _overworldStateHandler.StoreBerryTreeData(treeData);
        }
        var storyObjectives = GetJsonFilesFromPath(_saveDataPath + GetSaveDirectory(SaveDataDirectory.StoryObjectives));
        foreach (var jsonFilePath in storyObjectives)
        {
            //do not change this
            var rawJson = File.ReadAllText(jsonFilePath);
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
        yield return new WaitForSeconds(0.25f);
    }
    private void LoadPlayerData()
    {
        var playerPath = _saveDataPath + GetSaveDirectory(SaveDataDirectory.Player);
        var playerList = GetJsonFilesFromPath(playerPath);
        
        if(playerList.Count==1)
        {
            _gameLoadingHandler.playerData  = LoadObjectFromJson<Player_data>(playerList[0]);
        }
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
        var itemList = GetJsonFilesFromPath(_saveDataPath+GetSaveDirectory(SaveDataDirectory.Items));
        var storageItemList = GetJsonFilesFromPath(_saveDataPath+GetSaveDirectory(SaveDataDirectory.StorageItems));
        _playerBagHandler.allItems.Clear();
        
        foreach (var itemPath in itemList)
        {
            var item = LoadObjectFromJson<Item>(_saveDataPath + GetSaveDirectory(SaveDataDirectory.Items) + Path.GetFileName(itemPath));
            item.LoadData();
            _playerBagHandler.allItems.Add(item);
        }
        foreach (var itemPath in storageItemList)
        {
            var item   = LoadObjectFromJson<Item>(_saveDataPath + GetSaveDirectory(SaveDataDirectory.StorageItems) +
                                                  Path.GetFileName(itemPath));
            item.LoadData();
            _playerBagHandler.storageItems.Add(item);
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
        _pokemonStorageHandler.totalPokemonCount = 0;
        _pokemonPartyHandler.numMembers = 0;
        
        var partyPokemonList = GetJsonFilesFromPath(_saveDataPath + GetSaveDirectory(SaveDataDirectory.PartyPokemon));
        
        _pokemonPartyHandler.numMembers = partyPokemonList.Count; 
        _pokemonStorageHandler.totalPokemonCount += partyPokemonList.Count;
        
        for (int i = 0; i < partyPokemonList.Count; i++)
        {
            var pokemon = LoadObjectFromJson<Pokemon>(partyPokemonList[i]);
            pokemon.LoadDataAndDependencies(_container);
            LoadHeldItems(pokemon);
            _pokemonPartyHandler.party[i] = pokemon;
        }
        
        _pokemonStorageHandler.nonPartyPokemon.Clear();
        var storagePokemonList = GetJsonFilesFromPath(_saveDataPath + GetSaveDirectory(SaveDataDirectory.StoragePokemon));
        
        foreach (var file in storagePokemonList)
        {
            var fileName = Path.GetFileName(file);//filename is the pokemon id
            
            var nonPartyPokemon = LoadObjectFromJson<Pokemon>(_saveDataPath+ GetSaveDirectory(SaveDataDirectory.StoragePokemon) + fileName);
            nonPartyPokemon.LoadDataAndDependencies(_container);
            LoadHeldItems(nonPartyPokemon);
            _pokemonStorageHandler.nonPartyPokemon.Add(nonPartyPokemon);
            _pokemonStorageHandler.numNonPartyPokemon++;
            _pokemonStorageHandler.totalPokemonCount++;
        }
    }
    private void LoadHeldItems(Pokemon pokemon)
    {
        if (!pokemon.hasItem) return;
        var heldItemList = GetJsonFilesFromPath(_saveDataPath+GetSaveDirectory(SaveDataDirectory.HeldItems));
        if (heldItemList.Count > 0)
        {
            var heldItemPath = heldItemList
                .FirstOrDefault(path => 
                    RemoveFileExtension(Path.GetFileName(path)) 
                    == pokemon.pokemonID.ToString());
            
            if(string.IsNullOrEmpty(heldItemPath)) return;
            
            var heldItem = LoadObjectFromJson<Item>(heldItemPath); 
            heldItem.LoadData();
            pokemon.GiveItem(heldItem);
        }
    }
    
    private void ClearDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
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
            yield return CreateDefaultWebglDirectories();
            yield return new WaitUntil(() => _virtualFileStructureReady);
        }
        
        _inputStateHandler.ResetRelevantUi(InputStateName.PlayerMenu);
        _inputStateHandler.AddDialoguePlaceHolderState();
        _dialogueHandler.DisplayDetails("Saving...",false); 
        
        for (int i = 0; i < _pokemonPartyHandler.numMembers; i++)
        {
            try
            {
                var pokemon = _pokemonPartyHandler.party[i];
                if(pokemon==null) throw new Exception("pokemon is null! ");
              
                pokemon.SaveUnserializableData();
                if(pokemon.hasItem)
                {
                    if(pokemon.heldItem==null) throw new Exception("held Item is null! , for pokemon: "+pokemon.pokemonName); 
                    SaveDataAsJson(pokemon.heldItem, pokemon.pokemonID.ToString(), SaveDataDirectory.HeldItems);
                }
                SaveDataAsJson(pokemon, pokemon.pokemonID.ToString(), SaveDataDirectory.PartyPokemon);
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
                var pokemon = _pokemonStorageHandler.nonPartyPokemon[i];
                if(pokemon==null) throw new Exception("pokemon is null! ");
              
                pokemon.SaveUnserializableData();
                if(pokemon.hasItem)
                {
                    if(pokemon.heldItem==null) throw new Exception("held Item is null! , for pokemon: "+pokemon.pokemonName); 
                    SaveDataAsJson(pokemon.heldItem, pokemon.pokemonID.ToString(), SaveDataDirectory.HeldItems);
                }
                SaveDataAsJson(pokemon, pokemon.pokemonID.ToString(), SaveDataDirectory.StoragePokemon);
            }
            catch (Exception e)
            {
                OnSaveDataFail?.Invoke("Error occured with SaveNonPartyPokemonData, exception: ",e);
                yield break;
            }
        }

        for (var i = 0; i < _playerBagHandler.allItems.Count; i++)
        {
            var item = _playerBagHandler.allItems[i];
            if(item==null) throw new Exception("Item is null! ,index: "+i); 
            
            try
            {
                item.SaveModuleNames();
                item.DetermineImageDirectory();
                SaveDataAsJson(item, item.itemID,SaveDataDirectory.Items);
            }
            catch (Exception e)
            {
                OnSaveDataFail?.Invoke("Error occured with SaveItemDataAsJson, exception: ",e);
                yield break;
            }
        }
        
        for (var i = 0; i < _playerBagHandler.storageItems.Count; i++)
        {
            var item = _playerBagHandler.storageItems[i];
            if(item==null) throw new Exception("Storage item is null! ,index: "+i);
            try
            {
                SaveDataAsJson(item, item.itemID,SaveDataDirectory.StorageItems);
            }
            catch (Exception e)
            {
                OnSaveDataFail?.Invoke("Error occured with SaveStorageItem, exception: ",e);
                yield break;
            }
        }
        
        try
        {
            var player = _gameLoadingHandler.playerData;
            if(player==null) throw new Exception("player data is null! ");
            _gameLoadingHandler.playerData.playerPosition = _playerMovementHandler.GetPlayerPosition();
            _gameLoadingHandler.playerData.location = _areaHandler.currentArea.data.areaName;
            
            SaveDataAsJson(player, player.trainerID.ToString(),SaveDataDirectory.Player);
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

    private void SaveDataAsJson<T>(T saveSataObject, string fileName,SaveDataDirectory saveDirectory)
    {
        var directory = Path.Combine(_tempSaveDataPath+GetSaveDirectory(saveDirectory), fileName + ".json");
        var json = JsonUtility.ToJson(saveSataObject, true);
        File.WriteAllText(directory, json);
    }
    private T LoadObjectFromJson<T>(string filePath) where T : ScriptableObject
    {
        var json = File.ReadAllText(filePath);
        var jsonAsObject = ScriptableObject.CreateInstance<T>();
        JsonUtility.FromJsonOverwrite(json, jsonAsObject);
        return jsonAsObject;
    }
    
    public void SaveBerryTreeDataAsJson(BerryTreeData tree, string fileName)
    {
        SaveDataAsJson(tree,fileName,SaveDataDirectory.BerryTrees);
    }
    public void SaveStoryDataAsJson(StoryObjective objective, string fileName)
    {
        SaveDataAsJson(objective,fileName,SaveDataDirectory.StoryObjectives);
    }
    public void SaveStorageDataAsJson(PokemonStorageBox box, string fileName)
    {
        SaveDataAsJson(box,fileName,SaveDataDirectory.PCStorage);
    }
    public void SaveGameSettingsAsJson(SettingsConfig config, string fileName)
    {
        SaveDataAsJson(config,fileName,SaveDataDirectory.GameSettings);
    }
}