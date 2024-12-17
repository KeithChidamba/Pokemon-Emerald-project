using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Unity.Mathematics;
using UnityEngine;
[CreateAssetMenu(fileName = "Pokemon", menuName = "pkm")]
public class Pokemon : ScriptableObject
{
    public string Base_Pokemon_name;
    public string Pokemon_name;
    public string Pokemon_ID = "";
    public float HP;
    public float max_HP;
    public float Attack;
    public float Defense;
    public float SP_ATK;
    public float SP_DEF;
    public float speed;
    public float Accuracy = 100;
    public float Evasion = 100;
    public float crit_chance = 6.25f;
    public int Current_level = 1;
    public float level_progress = 0;
    public int base_exp_yield=0;
    public bool has_trainer=false;
    public bool canAttack = true;
    public bool isFlinched = false;
    public bool CanBeDamaged = true;
    public List<Type> types;
    public string Status_effect = "None";
    public string Buff_Debuff = "None";
    public string type_Immunity = "None";
    public string[] evo_line;
    public string[] learnSet;
    public List<Move> move_set=new();
    public Ability ability;
    public List<Evolution> evolutions;
    public Sprite front_picture;
    public Sprite back_picture;

    //data conversion when json to obj
    public string ability_name;
    public List<string> evo_data=new();
    public List<string> type_data=new();
    public List<string> move_data=new();
    public List<int> move_pp_data=new();
    public void Set_class_data()
    {
        ability_name = ability.ability;
        move_data.Clear();
        type_data.Clear();
        move_data.Clear();
        move_pp_data.Clear();
        evo_data.Clear();
        foreach (Move m in move_set)
        {
            move_data.Add(m.Move_name + "/" + m.type.Type_name);
            move_pp_data.Add(m.Powerpoints);
        }
        foreach (Type t in types)
            type_data.Add(t.Type_name);
        foreach (Evolution e in evolutions)
            evo_data.Add(e.Evo_name);
    }
    public void Set_Data(pokemon_storage storage)
    {
        front_picture = Resources.Load<Sprite>("Pokemon_project_assets/pokemon_img/" + Pokemon_name.ToLower());
        back_picture = Resources.Load<Sprite>("Pokemon_project_assets/pokemon_img/" + Pokemon_name.ToLower() + "_b");
        ability = Resources.Load<Ability>("Pokemon_project_assets/Pokemon_obj/Abilities/" + ability_name.ToLower());
        move_set.Clear();
        types.Clear();
        evolutions.Clear();
        for (int i = 0; i < move_data.Count; i++)
        {
            int pos = move_data[i].IndexOf('/')+1;
            string name_ = move_data[i].Substring(0, pos - 1).ToLower();
            string type = move_data[i].Substring(pos,move_data[i].Length - pos).ToLower();
            Move move_copy = Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + type + "/" + name_);
            move_copy.Powerpoints = move_pp_data[i];
            move_set.Add(Obj_Instance.set_move(move_copy));//check here
        }
        foreach (String t in type_data)
            types.Add(Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/" + t.ToLower()));
        foreach (String e in evo_data)
             evolutions.Add(Resources.Load<Evolution>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + Base_Pokemon_name + "/" +e));
        for(int i =0; i < types.Count; i++)
            types[i].type_img = Resources.Load<Sprite>("Pokemon_project_assets/ui/" + type_data[i].ToLower());
    }
    void Check_evolution()
    {
        for (int i = 0; i < evo_line.Length; i++)
        {
            int lv = int.Parse(evo_line[i]);
            if (Current_level == lv)
                Evolve(evolutions[i]);
        }
    }
    public void Get_exp(Pokemon enemy)
    {
        int level_difference = Current_level - enemy.Current_level;
        float trainer_bonus = 0;
        int exp;
        if (enemy.has_trainer)
            trainer_bonus = 1.5f;
        if (level_difference < 0)//enemy is stronger, so more exp
            exp = (int)math.floor(enemy.base_exp_yield * level_difference * trainer_bonus);
        else
            exp = (int)(math.floor((enemy.base_exp_yield * trainer_bonus)/level_difference) );
        level_progress += exp;
        if (level_progress >= 100)
        {
            int num_levels = (int)math.trunc(level_progress / 100);
            for (int i = 0; i < num_levels; i++)
                Level_up();
            level_progress -= 100 * num_levels;
        }
    }
    void Evolve(Evolution evo)
    {
        Pokemon_name = evo.Evo_name;
        types = evo.types;
        ability = evo.ability;
        learnSet = evo.learnSet;
        front_picture = evo.front_picture;
        back_picture = evo.back_picture;
        Attack += 3;
        SP_ATK += 3;
        Defense += 3;
        SP_DEF += 3;
        speed += 3;
    }
    public void Level_up()
    {
        Current_level++;
        Attack++;
        SP_ATK++;
        Defense++;
        SP_DEF++;
        speed++;
        max_HP += (float)math.round(0.5 * Current_level);
        Check_evolution();
        foreach (String l in learnSet)
        {
            int lv = int.Parse(l.Substring(l.Length - 2, 2));
            if (Current_level == lv)
            {
                int pos = l.IndexOf('/')+1;
                string t = l.Substring(0, pos - 1).ToLower(); //move type 
                string n = l.Substring(pos, l.Length - 2 - pos).ToLower();//move name
                if (move_set.Count==4)//new move ui, allow player to replace move or reject new move
                    move_set[move_set.Count-1] = Obj_Instance.set_move(Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + t + "/" + n));
                else
                    move_set.Add(Obj_Instance.set_move(Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + t + "/" + n)));
                break;
            }
        }
    }
}
