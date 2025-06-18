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
    public event Action OnVirtualFileSystemLoaded;
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
        _saveDataPath = GetSavePath();
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            Game_Load.Instance.uploadButton.interactable = false;
            LoadPlayerData(); 
            LoadItemData();
            LoadPokemonData();
        }
        else
        {
            Game_Load.Instance.uploadButton.interactable = true;
            Game_Load.Instance.PreventGameLoad();
        }
        
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
        OnVirtualFileSystemLoaded?.Invoke();
    }
    public void OnDownloadComplete()//js notification
    {
        Dialogue_handler.Instance.DisplayInfo("Save data downloaded successfully!", "Details");
    }
    public void OnIDBFSReady()//js notification
    {
        StartCoroutine(SyncFromIndexedDB());
    }
    IEnumerator SyncFromIndexedDB()
    {
        Dialogue_handler.Instance.DisplayInfo("Game Loaded","Details");
        LoadPlayerData(); 
        LoadItemData();
        LoadPokemonData();
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

    private void LoadPlayerData()
    {
        CreateFolder(_saveDataPath + "/Player");
        Game_Load.Instance.playerData = null;
        var playerList = GetJsonFilesFromPath(_saveDataPath + "/Player");
        if(playerList.Count==1)
            Game_Load.Instance.playerData = LoadPlayerFromJson(_saveDataPath+"/Player/" + Path.GetFileName(playerList[0]));
        else if (playerList.Count > 1)
        {
            Dialogue_handler.Instance.DisplayInfo("Please ensure only one player's data is in the save_data folder!","Details");
            Game_Load.Instance.PreventGameLoad();
        }
        else
        {
            Dialogue_handler.Instance.DisplayInfo("There was no save data found!","Details");
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
            pokemon.heldItem = (string.IsNullOrEmpty(heldItemID))? null : LoadItemFromJson(heldItemID);
        }
    }
    private void CreateFolder(string path)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer) return;
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
    public void EraseSaveData()
    {
        ClearDirectory(_saveDataPath+"/Pokemon");
        ClearDirectory(_saveDataPath+"/Items");
        ClearDirectory(_saveDataPath+"/Player");
        ClearDirectory(_saveDataPath+"/Party_Ids");
    }
    private void ClearDirectory(string path)
    {
        var files = Directory.GetFiles(path);
        foreach (var file in files)
            File.Delete(file);
    }

    public void SaveAllData()
    {
        Dialogue_handler.Instance.DisplayInfo("Saving...", "Details");
        EraseSaveData();
        for (int i = 0; i < pokemon_storage.Instance.numPartyMembers; i++)
        {
            Pokemon_party.Instance.party[i].SaveUnserializableData();
            if(Pokemon_party.Instance.party[i].hasItem)
                SaveHeldItem(Pokemon_party.Instance.party[i].heldItem, Pokemon_party.Instance.party[i].pokemonID.ToString());
            SavePokemonDataAsJson(Pokemon_party.Instance.party[i]);
        }
        for (int i = 0; i < pokemon_storage.Instance.numNonPartyPokemon; i++)
        {
            pokemon_storage.Instance.nonPartyPokemon[i].SaveUnserializableData();
            if(pokemon_storage.Instance.nonPartyPokemon[i].hasItem)
                SaveHeldItem(pokemon_storage.Instance.nonPartyPokemon[i].heldItem, pokemon_storage.Instance.nonPartyPokemon[i].pokemonID.ToString());
            SavePokemonDataAsJson(pokemon_storage.Instance.nonPartyPokemon[i]);
        }
        foreach(var item in Bag.Instance.bagItems)
            SaveItemDataAsJson(item, item.itemID);
        Game_Load.Instance.playerData.playerPosition = Player_movement.Instance.transform.position;
        Game_Load.Instance.playerData.location = (Game_Load.Instance.playerData.location==string.Empty) ? 
            "Overworld" 
            : area.currentArea.areaName;
        SavePlayerDataAsJson(Game_Load.Instance.playerData,Game_Load.Instance.playerData.trainerID.ToString());
        SavePartyPokemonIDs();
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

    private void SavePartyPokemonIDs()
    {
        for (int i = 0; i < pokemon_storage.Instance.numPartyMembers; i++)
        {
            var path = Path.Combine(_saveDataPath + "/Party_Ids/", "pkm_" + (i + 1) + ".txt");
            File.WriteAllText(path, Pokemon_party.Instance.party[i].pokemonID.ToString());
        }
        #if UNITY_WEBGL && !UNITY_EDITOR
                                DownloadZipAndStoreLocally();
        #endif
        Dialogue_handler.Instance.DisplayInfo("Game saved online but please download your save file", "Details");
        Game_ui_manager.Instance.CloseMenu();
        
        if (Application.platform == RuntimePlatform.WebGLPlayer) return;
        
        Dialogue_handler.Instance.DisplayInfo("Game saved", "Details");
        Dialogue_handler.Instance.EndDialogue(1f);
    }

    private void SaveHeldItem(Item itm, string fileName)
    {
        var directory = Path.Combine(_saveDataPath+"/Items/Held_Items", fileName + ".json");
        var json = JsonUtility.ToJson(itm, true);
        File.WriteAllText(directory, json);
    }
    private void SavePokemonDataAsJson(Pokemon pokemon)
    {
        var directory = Path.Combine(_saveDataPath+"/Pokemon/", pokemon.pokemonID + ".json");
        var json = JsonUtility.ToJson(pokemon, true);
        File.WriteAllText(directory, json);
    }
    private void SavePlayerDataAsJson(Player_data player, string fileName)
    {
        var directory = Path.Combine(_saveDataPath+"/Player", fileName + ".json");
        var json = JsonUtility.ToJson(player, true);
        File.WriteAllText(directory, json);
    }
    private void SaveItemDataAsJson(Item itm, string fileName)
    {
        var directory = Path.Combine(_saveDataPath+"/Items", fileName + ".json");
        var json = JsonUtility.ToJson(itm, true);
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
        item.itemImage = Testing.CheckImage("Pokemon_project_assets/ui/" ,item.itemName);
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
