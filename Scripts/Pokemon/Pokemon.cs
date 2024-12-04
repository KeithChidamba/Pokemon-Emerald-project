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
    public int Current_level = 1;
    public float level_progress = 0;
    public int base_exp_yield=0;
    public bool has_trainer=false;
    public Type[] types;
    public string Status_effect = "None";
    public string type_Immunity = "None";
    public string[] evo_line;
    public string[] learnSet;
    public Move[] move_set = { null, null, null, null };
    public Ability ability;
    public Evolution[] evolutions;
    public Sprite front_picture;
    public Sprite back_picture;

    //data conversion when json to obj
    public string ability_name;
    public string[] evo_data = { "", "" };
    public string[] type_data = {"",""};
    public string[] move_data = { "", "", "", "" };
    public int[] move_pp_data = { 0, 0, 0, 0 };
    public int num_moves=0;
    public int num_types=0;
    public int num_evo=0;
    public void Set_class_data()
    {
        ability_name = ability.ability;
        num_moves = 0;
        for (int i = 0; i < move_set.Length; i++)
        {
            if (move_set[i] != null)
            {
                num_moves++;
                move_data[i] = move_set[i].Move_name + "/" + move_set[i].type.Type_name;
                move_pp_data[i] = move_set[i].Powerpoints;
            }
            else
            {
                move_data[i] = "";
            }
        }
        int j = 0;
        foreach (Type t in types)
        {
            type_data[j] = t.Type_name;
            j++;
        }
        num_types = j;
        int k = 0;
        foreach (Evolution e in evolutions)
        {
            evo_data[k] = e.Evo_name;
            k++;
        }
        num_evo = k;
    }
    int pos_spliter(int index, string[] arr)
    {
        int pos = 0;
        char splitter = '/';
        for (int i = 0; i < arr[index].Length; i++)
        {
            if (arr[index][i] == splitter)
            {
                pos = i+1;
            }
        }
        return pos;
    }
    public void Set_Data(pokemon_storage storage)
    {
        front_picture = Resources.Load<Sprite>("Pokemon_project_assets/pokemon_img/" + Pokemon_name.ToLower());
        back_picture = Resources.Load<Sprite>("Pokemon_project_assets/pokemon_img/" + Pokemon_name.ToLower() + "_b");

        ability = Resources.Load<Ability>("Pokemon_project_assets/Pokemon_obj/Abilities/" + ability_name.ToLower());
        for (int i = 0; i < num_moves; i++)
        {
            int pos = pos_spliter(i, move_data);
            string name_ = move_data[i].Substring(0, pos - 1).ToLower();
            string type = move_data[i].Substring(pos,move_data[i].Length - pos).ToLower();
            Move move_copy = Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + type + "/" + name_);
            move_set[i] = storage.options.ins_manager.set_move(move_copy);
            move_set[i].Powerpoints = move_pp_data[i];
        }
        for (int i = 0; i < num_types; i++)
        {
            types[i] = Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/" + type_data[i].ToLower());
        }
        for (int i = 0; i < num_evo; i++)
        {
             evolutions[i] = Resources.Load<Evolution>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + Base_Pokemon_name + "/" +evo_data[i]);
        }
        int j = 0;
        foreach (Type t in types)
        {
            t.type_img = Resources.Load<Sprite>("Pokemon_project_assets/ui/" + type_data[j].ToLower());
            j++;
        }
    }
    void Check_evolution()
    {
        for (int i = 0; i < evo_line.Length; i++)
        {
            int lv = int.Parse(evo_line[i]);
            if (Current_level == lv)
            {
                Evolve(evolutions[i]);
            }
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
        { exp = (int)math.floor(enemy.base_exp_yield * level_difference * trainer_bonus);
        }
        else
        {
             exp = (int)(math.floor((enemy.base_exp_yield * trainer_bonus)/level_difference) );
        }
        level_progress += exp;
        if (level_progress >= 100)
        {
            int num_levels = (int)math.trunc(level_progress / 100);
            for (int i = 0; i < num_levels; i++)
            {
                Level_up();
            }
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
        int num_moves = 0;
        foreach (Move m in move_set)
        {
            if (m != null)
            {
                num_moves++;
            }
        }
        for (int i = 0; i < learnSet.Length; i++)
        {
            int lv = int.Parse(learnSet[i].Substring(learnSet[i].Length - 2, 2));
            if (Current_level == lv)
            {
                int pos = pos_spliter(i, learnSet);
                string t = learnSet[i].Substring(0, pos - 1).ToLower(); //move type 
                string n = learnSet[i].Substring(pos, learnSet[i].Length - 2 - pos).ToLower();//move name
                if (num_moves == 4)
                {
                    move_set[num_moves-1] = Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + t + "/" + n);
                }
                else
                {
                    move_set[num_moves] = Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + t + "/" + n);
                }

                break;
            }
            
        }
    }
}
