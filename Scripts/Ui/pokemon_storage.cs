using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class pokemon_storage : MonoBehaviour
{
    public List<Pokemon> non_party_pokemon;
    public int num_pokemon = 0;
    public int num_non_party_pokemon = 0;
    public int max_num_pokemon = 40;
    public int num_party_members = 0;
    public Button[] Storage_options;
    public GameObject storage_ui;
    public string select_pkm_ID;
    public bool storage_operetation = false;
    public bool using_pc = false;
    public List<GameObject> pkm_icons;
    public GameObject[] pkm_party_icons;
    public GameObject pkm_icon;
    public Transform storage_positions;
    public bool swapping = false;
    public bool Pkm_selected = false;
    public static pokemon_storage instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    Pokemon search_pkm(string ID)
    {
        foreach(Pokemon mon in non_party_pokemon)
            if (mon.Pokemon_ID.ToString() == ID)
                return mon;
        return null;
    }
    int search_pkm_pos(string ID)
    {
        int i = 0;
        foreach (Pokemon mon in non_party_pokemon)
        {
            if (mon.Pokemon_ID.ToString() == ID)
                return i;
            i++;
        }
        return 0;
    }
    public bool IsPartyPokemon(string id)
    {
        foreach (var pokemonID in Save_manager.Instance.partyIDs)
            if (pokemonID == id)
                return true;
        return false;
    }
    public void Open_pc()
    {
        using_pc = true;
        Set_pkm_icon();
        storage_ui.SetActive(true);
        Dialogue_handler.instance.Dialouge_off();
        DisableOptions();
    }
    public void Close_pc()
    {
        foreach (GameObject pos in pkm_party_icons)
            pos.GetComponent<PC_party_pkm>().options.SetActive(false);
        Game_ui_manager.Instance.ResetPlayerMovement();
        using_pc = false;
        Pkm_selected = false;
        storage_ui.SetActive(false);
        Remove_pkm_icons();
    }
    public void Select_party_pkm(PC_party_pkm party_pkm)
    {
        if (swapping)
        {
            storage_operetation = true;
            Pokemon store = Pokemon_party.instance.party[party_pkm.party_pos - 1];
            Pokemon_party.instance.party[party_pkm.party_pos - 1] = Add_pokemon(search_pkm(select_pkm_ID));
            non_party_pokemon[search_pkm_pos(select_pkm_ID)] = Add_pokemon(store);
            swapping = false;
            Remove_pkm_icons();
            Set_pkm_icon();
            storage_operetation = false;
            Pkm_selected = false;
        }
        else
        {
            if (!Pkm_selected)
            {
                ResetPartyUi();
                party_pkm.options.SetActive(true);
            }
        }
    }
    void LoadOptions()
    {
        foreach (GameObject icon in pkm_icons)
            icon.GetComponent<PC_pkm>().pkm_sprite.color=Color.HSVToRGB(0,0,100);
        foreach (var btn in Storage_options)
            btn.interactable = true;
    }
    void DisableOptions()
    {
        foreach (var btn in Storage_options)
            btn.interactable = false;
        ResetPartyUi();
    }
    void ResetPartyUi()
    {
        foreach (GameObject icon in pkm_party_icons)
            icon.GetComponent<PC_party_pkm>().options.SetActive(false);
    }
    public void RefreshUi()
    {
        DisableOptions();
        Remove_pkm_icons();
        Set_pkm_icon();
        Pkm_selected = false;
        swapping = false;
    }
    public void View_pc_pkm_details()
    {
        if (Pkm_selected && !swapping)
            Pokemon_Details.instance.Load_Details(search_pkm(select_pkm_ID));
    }
    public void Select_pc_pkm(PC_pkm pkm_icon)
    {
        if (!swapping)
        {
            DisableOptions();
            LoadOptions();
            Pkm_selected = true;
            select_pkm_ID = pkm_icon.pkm.Pokemon_ID.ToString();
            pkm_icon.pkm_sprite.color=Color.HSVToRGB(17,96,54);
        }
    }
    void Remove_pkm_icons()
    {
        pkm_icons.Clear();
        for(int i = 0;i<storage_positions.childCount;i++)
            Destroy(storage_positions.GetChild(i).GetChild(0).gameObject);
        foreach (GameObject pos in pkm_party_icons)
        {
            pos.SetActive(false);
            pos.GetComponent<PC_party_pkm>().pkm = null;
        }
    }
    private void Set_pkm_icon()
    {
        int num = 0;
        foreach (Transform pos in storage_positions)
        {
            GameObject pkm = Instantiate(pkm_icon, pos);
            pkm_icons.Add(pkm);
        }
        foreach (Pokemon mon in non_party_pokemon)
        {
            if (mon != null)
            {
                pkm_icons[num].SetActive(true);
                pkm_icons[num].GetComponent<PC_pkm>().pkm = mon;
                pkm_icons[num].GetComponent<PC_pkm>().Load_image();
                storage_operetation = true;
                num++;
            }
        }
        num = 0;
        foreach (Pokemon mon in Pokemon_party.instance.party)
        {
            if (mon != null)
            {
                pkm_party_icons[num].SetActive(true);
                pkm_party_icons[num].GetComponent<PC_party_pkm>().pkm = mon;
                pkm_party_icons[num].GetComponent<PC_party_pkm>().Load_image();
                storage_operetation = true;
                num++;
            }
        }
        storage_operetation = false;
        storage_positions.gameObject.SetActive(true);
    }
    public void RemoveFromParty(PC_party_pkm pkm)
    {
        if (num_party_members > 1)
        {
            Pokemon_party.instance.Remove_Member(pkm.party_pos);
            num_party_members--;
            num_non_party_pokemon++;
            RefreshUi();
        }
        else
            Dialogue_handler.instance.Write_Info("There must be at least 1 pokemon in your team","Details",1f);
    }
    public void Delete_pkm()
    {
        int index = 0;
        foreach(Pokemon mon in non_party_pokemon)
        {
            if (mon.Pokemon_ID.ToString() == select_pkm_ID)
            {
                string pkm_name = search_pkm(select_pkm_ID).Pokemon_name;
                non_party_pokemon[index] = null;
                remove_pkm(index);
                Dialogue_handler.instance.Write_Info("You released "+ pkm_name, "Details",1.5f);
                RefreshUi();
                break;
            }
            index++;
        }
    }
    void remove_pkm(int empty_position)
    {
        non_party_pokemon.Remove(non_party_pokemon[empty_position]);
        num_non_party_pokemon--;
        num_pokemon--;
    }
    public void swap()
    {
        DisableOptions();
        storage_positions.gameObject.SetActive(false);
        Dialogue_handler.instance.Write_Info("Pick a pokemon in your party to swap with", "Details",1.2f);
        swapping = true;
    }
    public void  Add_to_Party()
    {
        if (Pokemon_party.instance.num_members < 6)
        {
            storage_operetation = true;
            Pokemon_party.instance.party[Pokemon_party.instance.num_members] = Add_pokemon(search_pkm(select_pkm_ID));
            //remove from box
            non_party_pokemon.Remove(search_pkm(select_pkm_ID));
            num_party_members++;
            num_non_party_pokemon--;
            Pokemon_party.instance.num_members++;
        }
        else
            Dialogue_handler.instance.Write_Info("Party is full, you can still swap out pokemon though", "Details",2f);
        storage_operetation = false;
        RefreshUi();
    }

    public Pokemon Add_pokemon(Pokemon pkm)
    {
        Pokemon new_pkm = Obj_Instance.CreatePokemon(pkm);
        new_pkm.has_trainer = true;
        if (!storage_operetation)
        {
            num_pokemon++;
            if (num_party_members < 6)
                num_party_members++;
            PokemonOperations.SetPokemonTraits(new_pkm);
            if (new_pkm.Current_level == 0)
                new_pkm.LevelUp();
        }
        return new_pkm;
    }


}
