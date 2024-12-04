using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Encounter_handler : MonoBehaviour
{
    public Encounter_Area area;
    public Transform encounter_triggers;
    public bool triggered_encounter = false;
    public Wild_pokemon_battle battle;
    public Pokemon wild_pkm;
    public Options_manager options;
    void Update()
    {
        
    }
    public void Trigger_encounter()
    {
        triggered_encounter = true;
        for (int i = 0; i < encounter_triggers.childCount; i++)
        {
            encounter_triggers.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < area.Pokemon.Length; i++)
        {
            int random = Random.Range(1, 101);
           // Debug.Log(area.Pokemon[i].Substring(0,5)+ " chance: " + int.Parse(area.Pokemon[i].Substring(area.Pokemon[i].Length - 3, 3)) + " roll: " + random);
           //send data to battle ui
           if(i == area.Pokemon.Length - 1)
            {
                string pkm_name = area.Pokemon[i].Substring(0, area.Pokemon[i].Length - 3);
                Create_pkm(pkm_name);
                break;
            }
            else
            {
                if (random < int.Parse(area.Pokemon[i].Substring(area.Pokemon[i].Length - 3, 3) ))
                {
                    
                    string pkm_name = area.Pokemon[i].Substring(0, area.Pokemon[i].Length - 3);
                    Create_pkm(pkm_name);
                    break;
                }
            }

        }
    }
    void Create_pkm(string pkm_name)
    {
        wild_pkm = FindObjectOfType<pokemon_storage>().Add_pokemon(Resources.Load<Pokemon>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + pkm_name.ToLower()));
        int rand_lv = Random.Range(area.min_lv, area.max_lv+1);
        for(int i=1;i<rand_lv;i++)
        {
            wild_pkm.Level_up();
        }
        battle.Start_Battle(wild_pkm);
    }
}
