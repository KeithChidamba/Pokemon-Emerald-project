using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
/// <summary>
/// Do not Rename the Save_Manager game object to anything else, jslib needs that name
/// </summary>
public class SaveDataHandler : MonoBehaviour,IInjectable
{
    [DllImport("__Internal")] private static extern void DownloadZipAndStoreLocally();
    [DllImport("__Internal")] private static extern void CreateDirectories(string jsonPtr);
    [DllImport("__Internal")] private static extern void UploadZipAndStoreToIDBFS();
    [DllImport("__Internal")] private static extern void ClearFileDataStore();

    private string _saveDataPath;
    private string _tempSaveDataPath;
    private event Action<string,Exception> OnSaveDataFail;
    public event Action OnUploadedDataReady;
    public event Action OnVirtualFsCreated;
    private bool _virtualFileStructureReady;
    private bool _virtualDirectoriesCleared;
    
    private DialogueHandler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private AreaManager  _areaHandler;
    private PokemonStorageHandler _pokemonStorageHandler;
    private GameLoadingHandler _gameLoadingHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    private PlayerMovementHandler _playerMovementHandler;
    private OverworldState _overworldStateHandler;
    private PlayerBagHandler _playerBagHandler;
    private GameSettingsHandler _gameSettingsHandler;
    private TestingEnvironmentHandler _testingHandler;
    private ServiceContainer _container;

    public void Inject(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _gameLoadingHandler = container.Resolve<GameLoadingHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _pokemonStorageHandler = container.Resolve<PokemonStorageHandler>();
        _playerMovementHandler = container.Resolve<PlayerMovementHandler>();
        _areaHandler = container.Resolve<AreaManager>();
        _overworldStateHandler = container.Resolve<OverworldState>();
        _playerBagHandler = container.Resolve<PlayerBagHandler>();
        _gameSettingsHandler = container.Resolve<GameSettingsHandler>();
        _testingHandler = container.Resolve<TestingEnvironmentHandler>();
        _container = container;
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        if (_testingHandler.environment == DevelopmentEnvironment.Testing) return;
        
        OnSaveDataFail += (errorMessage, exception) =>
        {
            Debug.LogError(errorMessage+exception);
            _dialogueHandler.DisplayDetails("Error occured while saving please restart the game!");
            EraseTemporarySaveData();
            _inputStateHandler.ResetSpecificUi(InputStateName.PlaceHolder);
        };
        
        switch (Application.platform)
        {
            case RuntimePlatform.WebGLPlayer:
                _saveDataPath = "/data/Save_data";
                _tempSaveDataPath = "/data/Temp_Save_data";
                break;
            default:
                _saveDataPath = "Assets/Save_data";
                _tempSaveDataPath ="Assets/Temp_Save_data";
                break;
        }
        
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            CreateAllSaveDirectories();
            _gameLoadingHandler.ShowMenuUI(ValidatePlayerData());
        }
        else
        {
            _gameLoadingHandler.ShowMenuUI(false);
        }
    }
    private bool ValidatePlayerData()
    {
        var playerPath = _saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.Player);
        var playerDataCount = GetJsonFilesFromPath(playerPath).Count;
        if(playerDataCount == 1)
        {
            return true;
        }
        if(playerDataCount > 1)
        {
            _dialogueHandler.DisplayDetails("Please ensure only one player's data is in the save_data folder! And Restart the game");
        }
        return false;
    }
    
    private void CreateAllSaveDirectories()
    {
        foreach (var dir in DirectoryHandler.SaveDataDirectories)
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
        foreach (var dir in DirectoryHandler.SaveDataDirectories)
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
//js notifications
    public void OnDownloadFailed()
    {
        _dialogueHandler.DisplayDetails("Download failed, check the browser console");
    }
    public void OnUploadFailed()
    {
        _dialogueHandler.DisplayDetails("Upload failed, check the browser console");
    }
    public void OnDirectoryCreationFailed()
    {
        _dialogueHandler.DisplayDetails("Could not create save folders");
    }
    public void OnIDBFSReady()//js notification
    {
        StartCoroutine(SyncFromIndexedDB());
    }
    private IEnumerator SyncFromIndexedDB()
    {
        _dialogueHandler.DisplayDetails("Save Loaded");
        OnUploadedDataReady?.Invoke();
        if (ValidatePlayerData())
        {
            LoadAllSaveData();
            yield return new WaitForSecondsRealtime(1f);
            _gameLoadingHandler.StartGame();
        }
    }
    /// <summary>
    /// Loads the player's items, pokemon and personal data from save files
    /// </summary>
    public void LoadAllSaveData()
    {
        //Load Player Data
        var playerPath = _saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.Player);
        var playerList = GetJsonFilesFromPath(playerPath);
        _gameLoadingHandler.playerData = LoadObjectFromJson<PlayerData>(playerList[0]);
        
        //Load Item Data
        var itemList = GetJsonFilesFromPath(_saveDataPath+DirectoryHandler.GetSaveDirectory(SaveDataDirectory.Items));
        var storageItemList = GetJsonFilesFromPath(_saveDataPath+DirectoryHandler.GetSaveDirectory(SaveDataDirectory.StorageItems));
        _playerBagHandler.allItems.Clear();
        
        foreach (var itemPath in itemList)
        {
            var item = LoadObjectFromJson<Item>(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.Items) + Path.GetFileName(itemPath));
            item.LoadData();
            _playerBagHandler.allItems.Add(item);
        }
        foreach (var itemPath in storageItemList)
        {
            var item = LoadObjectFromJson<Item>(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.StorageItems) +
                                                Path.GetFileName(itemPath));
            item.LoadData();
            _playerBagHandler.storageItems.Add(item);
        }
        //Load Pokemon Data
        var heldItemList = GetJsonFilesFromPath(_saveDataPath+DirectoryHandler.GetSaveDirectory(SaveDataDirectory.HeldItems));
        //Load party Pokemon
        _pokemonStorageHandler.totalPokemonCount = 0;
        var partyPokemonList = GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.PartyPokemon));
        _pokemonStorageHandler.totalPokemonCount += partyPokemonList.Count;
        
        foreach (var pokemonJson in partyPokemonList)
        {
            var pokemon = LoadObjectFromJson<Pokemon>(pokemonJson);
            pokemon.LoadDataAndDependencies(_container);
            LoadHeldItem(pokemon);
            _pokemonPartyHandler.AddMemberFromSystemProcess(pokemon);
        }
        //Load Storage Pokemon 
        _pokemonStorageHandler.nonPartyPokemon.Clear();
        var storagePokemonList = GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.StoragePokemon));
        foreach (var file in storagePokemonList)
        {
            var fileName = Path.GetFileName(file);//filename is the pokemon id
            
            var nonPartyPokemon = LoadObjectFromJson<Pokemon>(_saveDataPath+ DirectoryHandler.GetSaveDirectory(SaveDataDirectory.StoragePokemon) + fileName);
            nonPartyPokemon.LoadDataAndDependencies(_container);
            LoadHeldItem(nonPartyPokemon);
            _pokemonStorageHandler.nonPartyPokemon.Add(nonPartyPokemon);
            _pokemonStorageHandler.numNonPartyPokemon++;
            _pokemonStorageHandler.totalPokemonCount++;
        }
        return;
        void LoadHeldItem(Pokemon pokemon)
        {
            if (!pokemon.hasItem) return;
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
                
                heldItemList.Remove(heldItemPath);
            }
            return;
            string RemoveFileExtension(string filename)
            {
                return filename.Split('.')[0];
            }
        }
    }
    
    public List<SettingsConfig> GetSavedGameSettingsData()
    {
        var jsonFilesFromPath = GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.GameSettings));
        List<SettingsConfig> savedSettingConfigs = new();  
        foreach (var fullPath in jsonFilesFromPath)
        {
            var jsonFilePath = _saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.GameSettings) + Path.GetFileName(fullPath);
           
            if (!File.Exists(jsonFilePath)) continue;
              
            var json = File.ReadAllText(jsonFilePath);
           
            var configData = new SettingsConfig();
            JsonUtility.FromJsonOverwrite(json, configData);
            savedSettingConfigs.Add(configData);
        }
        return savedSettingConfigs;
    }
    public List<PokemonStorageBox> GetSavedPokemonStorageData()
    {
        var storageBoxes = GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.PCStorage));
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
        var overworldTrees = GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.BerryTrees));
        foreach (var jsonFilePath in overworldTrees)
        {
            var treeData = LoadObjectFromJson<BerryTreeData>(jsonFilePath);
            treeData.spriteData.Clear();
            var treeSprites = Resources.Load<BerryTreeData>(DirectoryHandler.GetDirectory(AssetDirectory.BerryTreeData) 
                                                            + $"{treeData.itemAssetName} Data").spriteData;
            treeData.spriteData = treeSprites;
            treeData.berryItem = Resources.Load<Item>(DirectoryHandler.GetDirectory(AssetDirectory.Items)
                                                      + treeData.itemAssetName);
            _overworldStateHandler.StoreBerryTreeData(treeData);
        }
        var storyObjectives = GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.StoryObjectives));
        foreach (var jsonFilePath in storyObjectives)
        {
            //do not change this
            var rawJson = File.ReadAllText(jsonFilePath);
            var save = new StoryObjectiveSave();
            JsonUtility.FromJsonOverwrite(rawJson,save);
            _overworldStateHandler.LoadStoryObjective(save);
        }
        
        var storyProgressJson = GetJsonFilesFromPath(_saveDataPath + 
                                                   DirectoryHandler.GetSaveDirectory(SaveDataDirectory.StoryObjectiveProgress));
        if (storyProgressJson.Count > 0)
        {
            StoryProgress storyProgress = new();
            var json = File.ReadAllText(storyProgressJson[0]);
            JsonUtility.FromJsonOverwrite(json, storyProgress);
            _overworldStateHandler.storyProgress = storyProgress;
        }
        
        var registryJson = GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.OverworldItemPickupRegistry));
        if(registryJson.Count > 0)
        {
            var pickupData = LoadObjectFromJson<OverworldPickupRegistry>(registryJson[0]);
            _overworldStateHandler.LoadItemPickups(pickupData);
        }
        yield return new WaitForSeconds(0.5f);
    }
    private List<string> GetJsonFilesFromPath(string path)
    {
        List<string> jsonFiles=new();
        var files = Directory.GetFiles(path);
       
        foreach(var file in files)
            if (Path.GetExtension(file) == ".json")
                jsonFiles.Add(file);
        return jsonFiles;
    }
    public void EraseSaveData()
    {
        foreach (var dir in DirectoryHandler.SaveDataDirectories)
        {
            DirectoryHandler.ClearDirectory(_saveDataPath + dir.Value);
        }
    }
    private void EraseTemporarySaveData()
    {
        foreach (var dir in DirectoryHandler.SaveDataDirectories)
        {
            DirectoryHandler.ClearDirectory(_tempSaveDataPath + dir.Value);
        }
    }
    public IEnumerator SaveAllData()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            _virtualFileStructureReady = false;
            yield return CreateDefaultWebglDirectories();
            yield return new WaitUntil(() => _virtualFileStructureReady);
        }
        else
        {
            CreateAllSaveDirectories();//just incase
        }
        
        _inputStateHandler.ResetSpecificUi(InputStateName.PlayerMenu);
        _inputStateHandler.AddPlaceHolderState();
        _dialogueHandler.DisplayDetails("Saving...",false); 
        
        foreach (var pokemon in _pokemonPartyHandler.Party)
        {
            try
            {
                if(pokemon is null) throw new Exception("pokemon is null! ");
              
                pokemon.SaveUnserializableData();
                if(pokemon.hasItem)
                {
                    if(pokemon.heldItem is null) throw new Exception("held Item is null! , for pokemon: "+pokemon.pokemonDisplayName); 
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
                if(pokemon is null) throw new Exception("pokemon is null! ");
              
                pokemon.SaveUnserializableData();
                if(pokemon.hasItem)
                {
                    if(pokemon.heldItem is null) throw new Exception("held Item is null! , for pokemon: "+pokemon.pokemonDisplayName); 
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
            if(item is null) throw new Exception("Item is null! ,index: "+i); 
            
            try
            {
                item.SetImageDirectory();
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
            if(item is null) throw new Exception("Storage item is null! ,index: "+i);
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
            if(player is null) throw new Exception("player data is null! ");
            _gameLoadingHandler.playerData.playerPosition = _playerMovementHandler.GetPlayerPosition();
            _gameLoadingHandler.playerData.location = _areaHandler.currentArea.locationData.areaName;
            
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
        }
        else
        {
            //empty old save data
            EraseSaveData();
            yield return new WaitForSecondsRealtime(1f);
            //copy new save data
            yield return DirectoryHandler.CopyDirectoryFiles(_tempSaveDataPath,_saveDataPath,recursive: true);
            yield return new WaitForSecondsRealtime(1f);
            SoundManager.Play(UiId.Save);
            EraseTemporarySaveData();
            _dialogueHandler.DisplayDetails("Game saved",false);
        }
        
        yield return new WaitForSecondsRealtime(1.5f);
        _dialogueHandler.EndDialogue();
        _inputStateHandler.ResetSpecificUi(InputStateName.PlaceHolder);
    }

    public void SaveDataAsJson<T>(T saveSataObject, string fileName,SaveDataDirectory saveDirectory)
    {
        var directory = Path.Combine(_tempSaveDataPath+DirectoryHandler.GetSaveDirectory(saveDirectory), fileName + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(directory) ?? "");//fail safe
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
}