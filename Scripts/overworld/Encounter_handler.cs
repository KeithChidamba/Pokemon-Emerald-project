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
        for (int i = 0; i < area.Pokemon.Length; i++)//send data to battle ui
        {
            int random = Random.Range(1, 101);
            int chance = int.Parse(area.Pokemon[i].Substring(area.Pokemon[i].Length - 3, 3));
            if ( (i == area.Pokemon.Length - 1) /*pick last option if none in range*/ || (random < chance) )//pick option within chance range
            {
                string pkm_name = area.Pokemon[i].Substring(0, area.Pokemon[i].Length - 3);
                Create_pkm(pkm_name);
                break;
            }
        }
    }
    void Create_pkm(string pkm_name)
    {
        wild_pkm = FindObjectOfType<pokemon_storage>().Add_pokemon(Resources.Load<Pokemon>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + pkm_name.ToLower()+"/"+ pkm_name.ToLower()));
        if (wild_pkm != null)
        {
            int rand_lv = Random.Range(area.min_lv, area.max_lv+1);
            for(int i=1;i<rand_lv;i++)
            {
                wild_pkm.Level_up();
            }
            battle.Start_Battle(wild_pkm);
        }
        else
        {
            Debug.Log("tried encounter but didnt find "+pkm_name);
        }
    }
}
