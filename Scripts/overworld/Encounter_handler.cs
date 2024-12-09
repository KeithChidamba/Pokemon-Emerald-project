using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Encounter_handler : MonoBehaviour
{
    public Encounter_Area area;
    public Transform encounter_triggers;
    public bool triggered_encounter = false;
    public Pokemon wild_pkm;
    public int encounter_chance = 2;

    public void Trigger_encounter()
    {
        triggered_encounter = true;
        encounter_chance = 2;
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
        wild_pkm = Obj_Instance.set_Pokemon(Resources.Load<Pokemon>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + pkm_name.ToLower()+"/"+ pkm_name.ToLower()));
        if (wild_pkm != null)
        {
            int rand_lv = Random.Range(area.min_lv, area.max_lv+1);
            for(int i=1;i<rand_lv;i++)
            {
                wild_pkm.Level_up();
            }
            wild_pkm.HP=wild_pkm.max_HP;
            //tests
           // battle.Start_Battle(wild_pkm);
           Battle_handler.instance.Start_Battle(new []{wild_pkm,wild_pkm},this);
        }
        else
        {
            Debug.Log("tried encounter but didnt find "+pkm_name);
        }
    }
    void trigger_off()
    {
        triggered_encounter = false;
    }
    public void Reset_trigger()
    {
        Invoke(nameof(trigger_off),1f);
    }
}
