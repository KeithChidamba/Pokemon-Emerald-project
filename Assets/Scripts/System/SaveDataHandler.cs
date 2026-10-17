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
public class SaveDataHandler : MonoBehaviour, IInjectable
{
    [DllImport("__Internal")]
    private static extern void DownloadZipAndStoreLocally();

    [DllImport("__Internal")]
    private static extern void CreateDirectories(string jsonPtr);

    [DllImport("__Internal")]
    private static extern void UploadZipAndStoreToIDBFS();

    [DllImport("__Internal")]
    private static extern void ClearFileDataStore();

    private string _saveDataPath;
    private string _tempSaveDataPath;
    private event Action<string, Exception> OnSaveDataFail;
    public event Action OnUploadedDataReady;
    public event Action OnVirtualFsCreated;
    private bool _virtualFileStructureReady;
    private bool _virtualDirectoriesCleared;

    private DialogueHandler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private AreaManager _areaHandler;
    private PokemonStorageHandler _pokemonStorageHandler;
    private GameLoadingHandler _gameLoadingHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    private PlayerMovementHandler _playerMovementHandler;
    private OverworldState _overworldStateHandler;
    private PlayerBagHandler _playerBagHandler;
    private GameSettingsHandler _gameSettingsHandler;
    private TestingEnvironmentHandler _testingHandler;
    
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
      
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        if (_testingHandler.environment == DevelopmentEnvironment.Testing) return;

        OnSaveDataFail += (errorMessage, exception) =>
        {
            Debug.LogError(errorMessage + exception);
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
                _tempSaveDataPath = "Assets/Temp_Save_data";
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
        if (playerDataCount == 1)
        {
            return true;
        }

        if (playerDataCount > 1)
        {
            _dialogueHandler.DisplayDetails(
                "Please ensure only one player's data is in the save_data folder! And Restart the game");
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

    public void OnFSCleared() //js notification
    {
        _virtualDirectoriesCleared = true;
    }

    public void OnFileStructureCreated() //js notification
    {
        _virtualFileStructureReady = true;
        OnVirtualFsCreated?.Invoke();
    }

    public void OnDownloadComplete() //js notification
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

    public void OnIDBFSReady() //js notification
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
        var itemRegistryPath = GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.Items))[0];
        _playerBagHandler.allItems.Clear();
        var itemJson = File.ReadAllText(itemRegistryPath);
        var itemRegistry = new ItemSaveData();
        JsonUtility.FromJsonOverwrite(itemJson, itemRegistry);
        _playerBagHandler.allItems.AddRange(itemRegistry.GetSaveItems());
        
        //Load Storage Item Data
        var storageItemRegistryPath =
            GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.StorageItems))[0];
        _playerBagHandler.storageItems.Clear();
        var storageItemJson = File.ReadAllText(storageItemRegistryPath);
        var storageItemRegistry = new ItemSaveData();
        JsonUtility.FromJsonOverwrite(storageItemJson, storageItemRegistry);
        _playerBagHandler.storageItems.AddRange(storageItemRegistry.GetSaveItems());
        
        //Load party Pokemon
        _pokemonStorageHandler.totalPokemonCount = 0;
        var partyPokemonList =
            GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.PartyPokemon));
        _pokemonStorageHandler.totalPokemonCount += partyPokemonList.Count;

        var unsortedPokemon = new List<(int index,Pokemon pokemon)>();
        
        var heldItemList =
                    GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.HeldItems));
        
        foreach (var jsonpath in partyPokemonList)
        {
            var savedPokemon = LoadSavedPokemon(jsonpath,heldItemList);
            var fileName = Path.GetFileName(jsonpath);
            //Format -> partyPosition[1-6] : ID
            var oderIndex = int.Parse(fileName.Substring(0,1));
            unsortedPokemon.Add((oderIndex,savedPokemon));
        }

        var sortedPartyPokemon = unsortedPokemon
            .OrderBy(group=> group.index).ToList();
        
        foreach (var sorted in sortedPartyPokemon)
        {
            _pokemonPartyHandler.AddMemberFromSystemProcess(sorted.pokemon);
        }
        
        //Load Storage Pokemon 
        _pokemonStorageHandler.nonPartyPokemon.Clear();
        var storagePokemonList =
            GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.StoragePokemon));
        foreach (var jsonpath in storagePokemonList)
        {
            var savedPokemon = LoadSavedPokemon(jsonpath,heldItemList);
            _pokemonStorageHandler.nonPartyPokemon.Add(savedPokemon);
            _pokemonStorageHandler.numNonPartyPokemon++;
            _pokemonStorageHandler.totalPokemonCount++;
        }
 
    }
    private Pokemon LoadSavedPokemon(string jsonpath,List<string> heldItemList)
    {
        var json = File.ReadAllText(jsonpath);
        PokemonSaveData save = new PokemonSaveData();
        JsonUtility.FromJsonOverwrite(json, save);
        
        var pokemonDirectory = DirectoryHandler.GetDirectory(AssetDirectory.Pokemon);
        var pokemonAssetCopy = InstanceFactory.CreatePokemon(Resources.Load<Pokemon>
            ($"{pokemonDirectory}{save.basePokemonName}/{save.basePokemonName}"));
            
        save.LoadUniqueData(pokemonAssetCopy);
        
        if (heldItemList.Count == 0) return pokemonAssetCopy;
            
        if (pokemonAssetCopy.hasItem)
        {
            var heldItemPath = heldItemList
                .FirstOrDefault(path =>
                    RemoveFileExtension(Path.GetFileName(path))
                    == pokemonAssetCopy.pokemonID.ToString());

            if (!string.IsNullOrEmpty(heldItemPath))
            {
                var heldItemJson = File.ReadAllText(heldItemPath);
                var itemSave = new SavedItem();
                JsonUtility.FromJsonOverwrite(heldItemJson, itemSave);
                    
                var heldItemAsset = Resources.Load<Item>(DirectoryHandler.GetDirectory(AssetDirectory.Items)
                                                         + itemSave.itemAssetName);
                    
                var heldItem = InstanceFactory.CreateItem(heldItemAsset);
                heldItem.quantity = itemSave.quantity;
                
                pokemonAssetCopy.GiveItem(heldItem);
                heldItemList.Remove(heldItemPath);
            }
        }
        return pokemonAssetCopy;
        string RemoveFileExtension(string filename)
        {
            return filename.Split('.')[0];
        }
    }
    public List<SettingsConfig> GetSavedGameSettingsData()
    {
        var jsonFilesFromPath =
            GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.GameSettings));
        List<SettingsConfig> savedSettingConfigs = new();
        foreach (var fullPath in jsonFilesFromPath)
        {
            var jsonFilePath = _saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.GameSettings) +
                               Path.GetFileName(fullPath);

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
        var storageBoxes =
            GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.PCStorage));
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
        var overworldTrees =
            GetJsonFilesFromPath(_saveDataPath + DirectoryHandler.GetSaveDirectory(SaveDataDirectory.BerryTrees));
        
        foreach (var jsonFilePath in overworldTrees)
        {
            var treeJson = File.ReadAllText(jsonFilePath);
            var treeSave = new BerryTreeSaveData();
            JsonUtility.FromJsonOverwrite(treeJson,treeSave);            
            _overworldStateHandler.StoreBerryTreeData(treeSave.LoadTree());
        }

        var storyProgressJson = GetJsonFilesFromPath(_saveDataPath +
                                                     DirectoryHandler.GetSaveDirectory(SaveDataDirectory
                                                         .StoryObjectiveProgress));
        if (storyProgressJson.Count > 0)
        {
            StoryProgress storyProgress = new();
            var json = File.ReadAllText(storyProgressJson[0]);
            JsonUtility.FromJsonOverwrite(json, storyProgress);
            _overworldStateHandler.storyProgress = storyProgress;
        }

        var registryJson = GetJsonFilesFromPath(_saveDataPath +
                                                DirectoryHandler.GetSaveDirectory(SaveDataDirectory
                                                    .OverworldItemPickupRegistry));
        if (registryJson.Count > 0)
        {
            var pickupData = LoadObjectFromJson<OverworldPickupRegistry>(registryJson[0]);
            _overworldStateHandler.LoadItemPickups(pickupData);
        }

        yield return new WaitForSeconds(0.5f);
    }

    private List<string> GetJsonFilesFromPath(string path)
    {
        List<string> jsonFiles = new();
        var files = Directory.GetFiles(path);

        foreach (var file in files)
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
            CreateAllSaveDirectories();
        }

        _inputStateHandler.ResetSpecificUi(InputStateName.PlayerMenu);
        _inputStateHandler.AddPlaceHolderState();
        _dialogueHandler.DisplayDetails("Saving...", false);

        for (int i = 0; i < _pokemonPartyHandler.Party.Count; i++)
        {
            try
            {
                var pokemon = _pokemonPartyHandler.Party[i];
                if (pokemon is null) throw new Exception("pokemon is null! ");
                
                var newPokemonSave = new PokemonSaveData();
                newPokemonSave.SetupSave(pokemon);
                if (pokemon.hasItem)
                {
                    if (pokemon.heldItem is null) throw new Exception("held Item is null! , for pokemon: " + pokemon.pokemonDisplayName);
                    
                    var itemSave = new SavedItem
                    {
                        itemAssetName = pokemon.heldItem.name,
                        quantity = pokemon.heldItem.quantity,
                    };
                    SaveDataAsJson(itemSave, pokemon.pokemonID.ToString(), SaveDataDirectory.HeldItems);
                }
                SaveDataAsJson(newPokemonSave, $"{i + 1}_{pokemon.pokemonID}", SaveDataDirectory.PartyPokemon);
            }
            catch (Exception e)
            {
                OnSaveDataFail?.Invoke("Error occured with SaveAllPokemonData, exception: ", e);
                yield break;
            }
        }

        for (int i = 0; i < _pokemonStorageHandler.numNonPartyPokemon; i++)
        {
            try
            {
                var pokemon = _pokemonStorageHandler.nonPartyPokemon[i];
                if (pokemon is null) throw new Exception("pokemon is null! ");

                var newPokemonSave = new PokemonSaveData();
                newPokemonSave.SetupSave(pokemon);
                
                if (pokemon.hasItem)
                {
                    if (pokemon.heldItem is null)
                        throw new Exception("held Item is null! , for pokemon: " + pokemon.pokemonDisplayName);
                    
                    var itemSave = new SavedItem
                    {
                        itemAssetName = pokemon.heldItem.name,
                        quantity = pokemon.heldItem.quantity,
                    };
                    SaveDataAsJson(itemSave, pokemon.pokemonID.ToString(), SaveDataDirectory.HeldItems);
                }
                SaveDataAsJson(newPokemonSave, pokemon.pokemonID.ToString(), SaveDataDirectory.StoragePokemon);
            }
            catch (Exception e)
            {
                OnSaveDataFail?.Invoke("Error occured with SaveNonPartyPokemonData, exception: ", e);
                yield break;
            }
        }
        
        try
        {
            var itemSave = new ItemSaveData();
            itemSave.SaveItemList(_playerBagHandler.allItems,"bag item is null! can't save");
            SaveDataAsJson(itemSave, "Item save registry", SaveDataDirectory.Items);
        }
        catch (Exception e)
        {
            OnSaveDataFail?.Invoke("Error occured with SaveItemDataAsJson, exception: ", e);
            yield break;
        }

        try
        {
            var itemSave = new ItemSaveData();
            itemSave.SaveItemList(_playerBagHandler.storageItems,"storage item is null! can't save");
            SaveDataAsJson(itemSave, "Storage item save registry", SaveDataDirectory.StorageItems);
        }
        catch (Exception e)
        {
            OnSaveDataFail?.Invoke("Error occured with SaveStorageItem, exception: ", e);
            yield break;
        }

        try
        {
            var player = _gameLoadingHandler.playerData;
            if (player is null) throw new Exception("player data is null! ");
            _gameLoadingHandler.playerData.playerPosition = _playerMovementHandler.GetPlayerPosition();
            _gameLoadingHandler.playerData.location = _areaHandler.currentArea.areaName;

            SaveDataAsJson(player, player.trainerID.ToString(), SaveDataDirectory.Player);
        }
        catch (Exception e)
        {
            OnSaveDataFail?.Invoke("Error occured with SavePlayerDataAsJson, exception: ", e);
            yield break;
        }
        try
        {
            _overworldStateHandler.SaveOverworldData();
        }
        catch (Exception e)
        {
            OnSaveDataFail?.Invoke("Error occured with SaveOverworldData, exception: ", e);
            yield break;
        }
        try
        {
            _pokemonStorageHandler.SaveStorageData();
        }
        catch (Exception e)
        {
            OnSaveDataFail?.Invoke("Error occured with SaveStorageData, exception: ", e);
            yield break;
        }
        try
        {
            _gameSettingsHandler.SaveSettings();
        }
        catch (Exception e)
        {
            OnSaveDataFail?.Invoke("Error occured with SaveSettings, exception: ", e);
            yield break;
        }
        yield return new WaitForSecondsRealtime(2f);
        
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
            yield return DirectoryHandler.CopyDirectoryFiles(_tempSaveDataPath, _saveDataPath, recursive: true);
            yield return new WaitForSecondsRealtime(1f);
            SoundManager.Play(UiId.Save);
            EraseTemporarySaveData();
            _dialogueHandler.DisplayDetails("Game saved", false);
        }

        yield return new WaitForSecondsRealtime(1f);
        _dialogueHandler.EndDialogue();
        _inputStateHandler.ResetSpecificUi(InputStateName.PlaceHolder);
    }

    public void SaveDataAsJson<T>(T saveSataObject, string fileName, SaveDataDirectory saveDirectory)
    {
        var directory = Path.Combine(_tempSaveDataPath + 
                                     DirectoryHandler.GetSaveDirectory(saveDirectory), fileName + ".json");
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