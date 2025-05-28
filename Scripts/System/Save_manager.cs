using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.Serialization;


public class Save_manager : MonoBehaviour
{
    [FormerlySerializedAs("party_IDs")] public List<string> partyIDs;
    public Area_manager area;
    public static Save_manager Instance { get; private set;}
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
        LoadItemData();    
        LoadPlayerData();
        LoadPokemonData();
    }
    private void LoadPlayerData()
    {
        CreateFolder("Assets/Save_data/Player");
        Game_Load.Instance.playerData = null;
        var playerList = GetJsonFilesFromPath("Assets/Save_data/Player");
        if(playerList.Count==1)
            Game_Load.Instance.playerData = LoadPlayerFromJson("Assets/Save_data/Player/" + Path.GetFileName(playerList[0]));
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
        CreateFolder("Assets/Save_data/Items");
        CreateFolder("Assets/Save_data/Items/Held_Items");
        Bag.Instance.bagItems.Clear();
        var itemList = GetJsonFilesFromPath("Assets/Save_data/Items");
        foreach(var item in itemList)
            Bag.Instance.bagItems.Add(LoadItemFromJson("Assets/Save_data/Items/" + Path.GetFileName(item)));
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
        CreateFolder("Assets/Save_data");
        CreateFolder("Assets/Save_data/Pokemon");
        CreateFolder("Assets/Save_data/Party_Ids");
        partyIDs.Clear();
        GetPartyPokemonIDs();
        for (int i = 0; i < pokemon_storage.Instance.numPartyMembers; i++)
        {
            var pokemon = LoadPokemonFromJson("Assets/Save_data/Pokemon/" + partyIDs[i] + ".json");
            LoadHeldItems(pokemon);
            Pokemon_party.Instance.party[i] = pokemon;
            Pokemon_party.Instance.numMembers++;
        }
        pokemon_storage.Instance.nonPartyPokemon.Clear();
        var pokemonList = GetJsonFilesFromPath("Assets/Save_data/Pokemon/");
        foreach(var file in pokemonList)
        {
            var fileName = Path.GetFileName(file);//filename is the pokemon id
            if (!pokemon_storage.Instance.IsPartyPokemon(RemoveFileExtension(fileName)))
            {
                var nonPartyPokemon = LoadPokemonFromJson("Assets/Save_data/Pokemon/" + fileName);
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
        var heldItemList = GetJsonFilesFromPath("Assets/Save_data/Items/Held_Items");
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
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
    public void EraseSaveData()
    {
        ClearDirectory("Assets/Save_data/Pokemon");
        ClearDirectory("Assets/Save_data/Items");
        ClearDirectory("Assets/Save_data/Player");
        ClearDirectory("Assets/Save_data/Party_Ids");
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
        for (int i = 0; i < Bag.Instance.numItems; i++)
            SaveItemDataAsJson(Bag.Instance.bagItems[i], Bag.Instance.bagItems[i].itemID);
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
        var files = Directory.GetFiles("Assets/Save_data/Party_Ids/");
        foreach(var file in files)
            if (GetFileExtension(file) == ".txt")
                numIds++;
        for (int i = 0; i < numIds; i++)
        {
            var currentID = File.ReadAllText("Assets/Save_data/Party_Ids/" + "pkm_" + (i + 1) + ".txt");
            partyIDs.Add(currentID);
            pokemon_storage.Instance.numPartyMembers++;
        }
    }
    private void SavePartyPokemonIDs()
    {
        for (int i = 0; i < pokemon_storage.Instance.numPartyMembers; i++)
        {
            var path = Path.Combine("Assets/Save_data/Party_Ids/", "pkm_" + (i + 1) + ".txt");
            File.WriteAllText(path, Pokemon_party.Instance.party[i].pokemonID.ToString());
        }
        Dialogue_handler.Instance.DisplayInfo("Game saved", "Details");
        Dialogue_handler.Instance.EndDialogue(1f);
        Game_ui_manager.Instance.CloseMenu();
    }

    private void SaveHeldItem(Item itm, string fileName)
    {
        var directory = Path.Combine("Assets/Save_data/Items/Held_Items", fileName + ".json");
        var json = JsonUtility.ToJson(itm, true);
        File.WriteAllText(directory, json);
    }
    private void SavePokemonDataAsJson(Pokemon pokemon)
    {
        var directory = Path.Combine("Assets/Save_data/Pokemon/", pokemon.pokemonID + ".json");
        var json = JsonUtility.ToJson(pokemon, true);
        File.WriteAllText(directory, json);
    }
    private void SavePlayerDataAsJson(Player_data player, string fileName)
    {
        var directory = Path.Combine("Assets/Save_data/Player", fileName + ".json");
        var json = JsonUtility.ToJson(player, true);
        File.WriteAllText(directory, json);
    }
    private void SaveItemDataAsJson(Item itm, string fileName)
    {
        var directory = Path.Combine("Assets/Save_data/Items", fileName + ".json");
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
        item.LoadImage();
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
