using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class Save_manager : MonoBehaviour
{
    public pokemon_storage storage;
    public Dialogue_handler dialogue;
    public Options_manager options;
    public Game_ui_manager ui_m;
    public List<string> party_IDs;
    public Bag playerBag;
    List<string> json_files = new();
    public Game_Load game_start;
    public Area_manager area;
    private void Start()
    {
        try
        {
            Load_items();
            Load_player();
            Load_pkm();
        }
        catch
        {
            dialogue.Write_Info("There was an error in loading you save data!","Details");
            game_start.New_Player_Data();
        }
    }
    
    private void Load_player()
    {
        CreateFolder("Assets/Save_data/Player");
        options.player_data = null;
        int j = load_files("Assets/Save_data/Player");
        if(j==1)
        {
            options.player_data = Get_player("Assets/Save_data/Player/" + Path.GetFileName(json_files[0]));
        }
        else if(j>1)
        {
            dialogue.Write_Info("Please ensure only one player's data is in the save_data folder!","Details");
        }
        else
        {
            dialogue.Write_Info("There was no save data found!","Details");
            game_start.New_Player_Data();
        }
    }
    private void Load_items()
    {
        CreateFolder("Assets/Save_data/Items");
        playerBag.bag_items.Clear();
        int j = load_files("Assets/Save_data/Items");
        for (int i = 0; i < j; i++)
        {
            playerBag.bag_items.Add(Get_Item("Assets/Save_data/Items/" + Path.GetFileName(json_files[i])));
        }
    }
    private int load_files(string path)
    {
        json_files.Clear();
        string[] files = Directory.GetFiles(path);
        int j = 0;
        for (int i = 0; i < files.Length; i++)
        {
            if (files[i].Substring(files[i].Length - 5, 5) == ".json")
            {
                json_files.Add(files[i]);
                j++;
            }
        }
        return j;
    }
    private void Load_pkm()
    {
        CreateFolder("Assets/Save_data");
        CreateFolder("Assets/Save_data/Pokemon");
        CreateFolder("Assets/Save_data/Party_Ids");
        party_IDs.Clear();
        Get_Pokemon_ID();
        for (int i = 0; i < storage.num_party_members; i++)
        {
            storage.party_.party[i] = Get_Pokemon("Assets/Save_data/Pokemon/" + party_IDs[i] + ".json");
            storage.party_.num_members++;
        }
        storage.non_party_pokemon.Clear();
        storage.all_pokemon.Clear();
        int j = load_files("Assets/Save_data/Pokemon/");
        for (int i = 0; i < j; i++)
        {
            if (!storage.search_pkm_ID(Path.GetFileName(json_files[i]).Substring(0, Path.GetFileName(json_files[i]).Length - 5)))
            {
                storage.non_party_pokemon.Add(Get_Pokemon("Assets/Save_data/Pokemon/" + Path.GetFileName(json_files[i])));
            }
            storage.all_pokemon.Add(Get_Pokemon("Assets/Save_data/Pokemon/" + Path.GetFileName(json_files[i])));
            storage.num_pokemon++;
        }
    }
    private void CreateFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public void Erase_save()
    {
        Clear_directory("Assets/Save_data/Pokemon");
        Clear_directory("Assets/Save_data/Items");
        Clear_directory("Assets/Save_data/Player");
        Clear_directory("Assets/Save_data/Party_Ids");
    }
    private void Clear_directory(string path)
    {
        string[] files = Directory.GetFiles(path);
        foreach (string file in files)
        {
            File.Delete(file);
        }
    }
    public void Save_all()
    {
        dialogue.Write_Info("Saving...", "Details");
        Erase_save();
        for (int i = 0; i < storage.num_pokemon; i++)
        {
            storage.all_pokemon[i].Set_class_data();
            Save_Pokemon(storage.all_pokemon[i], storage.all_pokemon[i].Pokemon_ID);
        }
        for (int i = 0; i < playerBag.num_items; i++)
        {
            Save_Item(playerBag.bag_items[i], playerBag.bag_items[i].Item_ID);
        }
        options.player_data.player_Position = options.player.movement.transform.position;
        options.player_data.Location = area.current_area.area_name;
        Save_Player(options.player_data,options.player_data.Player_ID);
        Save_Pokemon_ID();
    }
    private void Get_Pokemon_ID()
    {
        storage.num_party_members = 0;
        int num_ids = 0;
        string[] files = Directory.GetFiles("Assets/Save_data/Party_Ids/");
        for (int i = 0; i < files.Length; i++)
        {
            if (files[i].Substring(files[i].Length - 4, 4) == ".txt")
            {
                num_ids++;
            }
        }
        for (int i = 0; i < num_ids; i++)
        {
            string ID = File.ReadAllText("Assets/Save_data/Party_Ids/" + "pkm_" + (i + 1).ToString() + ".txt");
            party_IDs.Add(ID);
            storage.num_party_members++;
        }
    }
    private void Save_Pokemon_ID()
    {
        for (int i = 0; i < storage.num_party_members; i++)
        {
            string path = Path.Combine("Assets/Save_data/Party_Ids/", "pkm_" + (i + 1).ToString() + ".txt");
            File.WriteAllText(path, storage.party_.party[i].Pokemon_ID);
        }
        dialogue.Write_Info("Game saved", "Details");
        dialogue.Dialouge_off(1f);
        ui_m.Menu_off();
    }
    private void Save_Pokemon(Pokemon pkm, string file_name)
    {
        string directory = Path.Combine("Assets/Save_data/Pokemon", file_name + ".json");
        string json = JsonUtility.ToJson(pkm, true);
        File.WriteAllText(directory, json);
    }
    private void Save_Player(Player_data player, string file_name)
    {
        string directory = Path.Combine("Assets/Save_data/Player", file_name + ".json");
        string json = JsonUtility.ToJson(player, true);
        File.WriteAllText(directory, json);
    }
    private void Save_Item(Item itm, string file_name)
    {
        string directory = Path.Combine("Assets/Save_data/Items", file_name + ".json");
        string json = JsonUtility.ToJson(itm, true);
        File.WriteAllText(directory, json);
    }
    private Pokemon Get_Pokemon(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            Pokemon pkm = ScriptableObject.CreateInstance<Pokemon>();
            JsonUtility.FromJsonOverwrite(json, pkm);
            pkm.Set_Data(storage);
            return pkm;
        }
        return null;
    }
    private Item Get_Item(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            Item itm = ScriptableObject.CreateInstance<Item>();
            JsonUtility.FromJsonOverwrite(json, itm);
            itm.Set_img();
            return itm;
        }
        return null;
    }
    private Player_data Get_player(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            Player_data player = ScriptableObject.CreateInstance<Player_data>();
            JsonUtility.FromJsonOverwrite(json, player);
            return player;
        }
        return null;
    }
}
