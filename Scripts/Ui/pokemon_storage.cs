using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class pokemon_storage : MonoBehaviour
{
    public List<Pokemon> all_pokemon;
    public List<Pokemon> non_party_pokemon;
    public int num_pokemon = 0;
    public int num_non_party_pokemon = 0;
    public int max_num_pokemon = 40;
    public int num_party_members = 0;
    public GameObject Storage_options;
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
    public Button view_details;
    [SerializeField]private Pokemon_Details details;
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
    private void Update()
    {
            view_details.interactable = Pkm_selected;
    }
    public string Generate_ID(string name_)//pokemon's unique ID
    {
        int rand = Utility.Get_rand(0,name_.Length);
        string end_digits = Utility.Get_rand(0,name_.Length).ToString() + Utility.Get_rand(0,name_.Length).ToString() + Utility.Get_rand(0,name_.Length).ToString() + Utility.Get_rand(0,name_.Length).ToString();
        string id = rand.ToString();
        id += name_[rand];
        if (rand >= name_.Length-1)
        {
            id += name_.Substring(rand-4, 3);
        }
        else
        {
            id += name_.Substring(rand, (name_.Length-1)-rand );
        }
        id += end_digits;
        return id;
    }
    Pokemon search_pkm(string ID)
    {
        foreach(Pokemon mon in non_party_pokemon)
        {
            if (mon.Pokemon_ID == ID)
            {
                return mon;
            }
        }
        return null;
    }
    int search_pkm_pos(string ID)
    {
        int i = 0;
        foreach (Pokemon mon in non_party_pokemon)
        {
            if (mon.Pokemon_ID == ID)
            {
                return i;
            }
            i++;
        }
        return 0;
    }
    public bool search_pkm_ID(string ID)
    {
        foreach (string mon_ID in Save_manager.instance.party_IDs)
        {
            if (mon_ID == ID)
            {
                return true;
            }
        }
        return false;
    }
    public void Open_pc()
    {
        using_pc = true;
        Set_pkm_icon();
        storage_ui.SetActive(true);
        Dialogue_handler.instance.Dialouge_off();
    }
    public void Close_pc()
    {
        foreach (GameObject pos in pkm_party_icons)
        {
                pos.GetComponent<PC_party_pkm>().options.SetActive(false);
        }
        Game_ui_manager.instance.Reset_player_movement();
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
                party_pkm.options.SetActive(true);
                foreach (GameObject pos in pkm_party_icons)
                {
                    if (pos != party_pkm.gameObject)
                    {
                        pos.GetComponent<PC_party_pkm>().options.SetActive(false);
                        break;
                    }

                }
            }
        }
    }
    public void Cancel_options()
    {
        foreach (GameObject pos in pkm_party_icons)
        {
                pos.GetComponent<PC_party_pkm>().options.SetActive(false);
        }
        Remove_pkm_icons();
        Set_pkm_icon();
        Pkm_selected = false;
        swapping = false;
    }
    public void View_pc_pkm_details()
    {
        if (Pkm_selected && !swapping)
        {
            details.gameObject.SetActive(true);
            details.Load_Details(search_pkm(select_pkm_ID));
        }
    }
    public void Select_pc_pkm(PC_pkm pkm_icon)
    {
        if (!swapping)
        {
            Pkm_selected = true;
            select_pkm_ID = pkm_icon.pkm.Pokemon_ID;
            pkm_icon.options.SetActive(true);
            pkm_icon.options.SetActive(true);
            foreach (GameObject pos in pkm_icons)
            {
                if (pos != pkm_icon.gameObject)
                {
                    pos.GetComponent<PC_pkm>().options.SetActive(false);
                    break;
                }

            }
        }
    }
    void Remove_pkm_icons()
    {
        pkm_icons.Clear();
        for(int i = 0;i<storage_positions.childCount;i++)
        {
            Destroy(storage_positions.GetChild(i).GetChild(0).gameObject);
        }
        foreach (GameObject pos in pkm_party_icons)
        {
            pos.SetActive(false);
            pos.GetComponent<PC_party_pkm>().pkm = null;
        }
    }
    public void Set_pkm_icon()
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
    public void Remove_pkm(PC_party_pkm pkm)
    {
        if (num_party_members > 1)
        {
            Pokemon_party.instance.Remove_Member(pkm.party_pos);
            num_party_members--;
            Cancel_options();
        }
        else
        {
            Dialogue_handler.instance.Write_Info("There must be at least 1 pokemon in your team","Details");
        }

    }
    public void Delete_pkm(PC_pkm pkm_icon)
    {
        int index = 0;
        foreach(Pokemon mon in all_pokemon)
        {
            if (mon.Pokemon_ID == pkm_icon.pkm.Pokemon_ID)
            {
                string pkm_name = pkm_icon.pkm.Pokemon_name;
                all_pokemon[index] = null;
                sort_icons(index);
                Dialogue_handler.instance.Write_Info("You released "+ pkm_name, "Details");
                Dialogue_handler.instance.Dialouge_off(1.5f);
                Cancel_options();
                break;
            }
            index++;
        }
    }
    void sort_icons(int empty_position)
    {
        if (empty_position < num_pokemon - 1)
        {
            for (int i = empty_position; i < num_pokemon - 1; i++)
            {
                all_pokemon[i] = all_pokemon[i + 1];
            }
            all_pokemon.Remove(all_pokemon[num_pokemon - 1]);
            num_pokemon--;
        }
        non_party_pokemon.Clear();
        num_non_party_pokemon = 0;
        foreach (Pokemon mon in all_pokemon)
        {
            if (!Save_manager.instance.party_IDs.Contains(mon.Pokemon_ID))
            {
                non_party_pokemon.Add(mon);
                num_non_party_pokemon++;
            }
        }
    }
    public void swap()
    {
        storage_positions.gameObject.SetActive(false);
        Dialogue_handler.instance.Write_Info("Pick a pokemon in your party to swap with", "Details");
        Dialogue_handler.instance.Dialouge_off(1.2f);
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
        {
            Dialogue_handler.instance.Write_Info("Party is full, you can still swap out pokemon though", "Details");
            Dialogue_handler.instance.Dialouge_off(2f);
        }
        storage_operetation = false;
        Cancel_options();
    }
    public Pokemon Add_pokemon(Pokemon pkm)
    {
        if (!storage_operetation)
        {
            num_pokemon++;
            if (num_party_members < 6)
            {
                num_party_members++;
            }
        }
        //add new pokemon
        Pokemon new_pkm = Obj_Instance.set_Pokemon(pkm);
        if (!storage_operetation)
        {
            new_pkm.Pokemon_ID = Generate_ID(new_pkm.Pokemon_name);
        }
        return new_pkm;
    }

}
