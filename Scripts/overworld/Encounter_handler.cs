using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Encounter_handler : MonoBehaviour
{
    public Encounter_Area current_area;
    public bool triggered_encounter = false;
    public Pokemon wild_pkm;
    public int encounter_chance = 2;
    public static Encounter_handler instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    public void Trigger_encounter(Encounter_Area area)
    {
        current_area = area;
        triggered_encounter = true;
        encounter_chance = 2;
        for (int i = 0; i < current_area.Pokemon.Length; i++)//send data to battle ui
        {
            int random = Utility.Get_rand(1,101);
            int chance = int.Parse(current_area.Pokemon[i].Substring(current_area.Pokemon[i].Length - 3, 3));
            if ( (i == current_area.Pokemon.Length - 1) /*pick last option if none in range*/ || (random < chance) )//pick option within chance range
            {
                string pkm_name = current_area.Pokemon[i].Substring(0, current_area.Pokemon[i].Length - 3);
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
            int rand_lv = Utility.Get_rand(current_area.min_lv, current_area.max_lv+1);
            for(int i=1;i<rand_lv;i++)
                wild_pkm.Level_up();
            wild_pkm.HP=wild_pkm.max_HP;
            Battle_handler.instance.is_trainer_battle = false;
           Battle_handler.instance.isDouble_battle = false;
           Battle_handler.instance.Start_Battle(wild_pkm);
        }
        else
            Debug.Log("tried encounter but didnt find "+pkm_name);
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
