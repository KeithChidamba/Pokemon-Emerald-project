using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class Save_manager : MonoBehaviour
{
    public List<string> party_IDs;
    List<string> json_files = new();
    public Area_manager area;
    public static Save_manager instance { get; private set;}
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    private void Start()
    {
            Load_items();
            Load_player();
            Load_pkm();
    }
    
    private void Load_player()
    {
        CreateFolder("Assets/Save_data/Player");
        Game_Load.instance.player_data = null;
        int j = load_files("Assets/Save_data/Player");
        if(j==1)
            Game_Load.instance.player_data = Get_player("Assets/Save_data/Player/" + Path.GetFileName(json_files[0]));
        else if(j>1)
            Dialogue_handler.instance.Write_Info("Please ensure only one player's data is in the save_data folder!","Details");
        else
        {
            Dialogue_handler.instance.Write_Info("There was no save data found!","Details");
            Game_Load.instance.New_Player_Data();
        }
    }
    private void Load_items()
    {
        CreateFolder("Assets/Save_data/Items");
        Bag.instance.bag_items.Clear();
        int j = load_files("Assets/Save_data/Items");
        for (int i = 0; i < j; i++)
            Bag.instance.bag_items.Add(Get_Item("Assets/Save_data/Items/" + Path.GetFileName(json_files[i])));
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
        for (int i = 0; i < pokemon_storage.instance.num_party_members; i++)
        {
            Pokemon_party.instance.party[i] = Get_Pokemon("Assets/Save_data/Pokemon/" + party_IDs[i] + ".json");
            Pokemon_party.instance.num_members++;
        }
        pokemon_storage.instance.non_party_pokemon.Clear();
        int j = load_files("Assets/Save_data/Pokemon/");
        for (int i = 0; i < j; i++)
        {
            string file_name = Path.GetFileName(json_files[i]);
            if (!pokemon_storage.instance.search_pkm_ID(file_name.Substring(0, file_name.Length - 5)))
            {
                pokemon_storage.instance.non_party_pokemon.Add(Get_Pokemon("Assets/Save_data/Pokemon/" + file_name));
                pokemon_storage.instance.num_non_party_pokemon++;
            }
            pokemon_storage.instance.num_pokemon++;
        }
    }
    private void CreateFolder(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
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
            File.Delete(file);
    }
    public void Save_all()
    {
        Dialogue_handler.instance.Write_Info("Saving...", "Details");
        Erase_save();
        for (int i = 0; i < pokemon_storage.instance.num_party_members; i++)
        {
            Pokemon_party.instance.party[i].Set_class_data();
            Save_Pokemon(Pokemon_party.instance.party[i]);
        }
        for (int i = 0; i < pokemon_storage.instance.num_non_party_pokemon; i++)
        {
            pokemon_storage.instance.non_party_pokemon[i].Set_class_data();
            Save_Pokemon(pokemon_storage.instance.non_party_pokemon[i]);
        }
        for (int i = 0; i < Bag.instance.num_items; i++)
        {
            Save_Item(Bag.instance.bag_items[i], Bag.instance.bag_items[i].Item_ID);
        }
        Game_Load.instance.player_data.player_Position = Player_movement.instance.transform.position;
        Game_Load.instance.player_data.Location = area.current_area.area_name;
        Save_Player(Game_Load.instance.player_data,Game_Load.instance.player_data.Trainer_ID.ToString());
        Save_Pokemon_ID();
    }
    private void Get_Pokemon_ID()
    {
        pokemon_storage.instance.num_party_members = 0;
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
            pokemon_storage.instance.num_party_members++;
        }
    }
    private void Save_Pokemon_ID()
    {
        for (int i = 0; i < pokemon_storage.instance.num_party_members; i++)
        {
            string path = Path.Combine("Assets/Save_data/Party_Ids/", "pkm_" + (i + 1).ToString() + ".txt");
            File.WriteAllText(path, Pokemon_party.instance.party[i].Pokemon_ID.ToString());
        }
        Dialogue_handler.instance.Write_Info("Game saved", "Details");
        Dialogue_handler.instance.Dialouge_off(1f);
        Game_ui_manager.instance.Menu_off();
    }
    private void Save_Pokemon(Pokemon pkm)
    {
        string directory = Path.Combine("Assets/Save_data/Pokemon/", pkm.Pokemon_ID + ".json");
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
            pkm.Set_Data(pokemon_storage.instance);
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
