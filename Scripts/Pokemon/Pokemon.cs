using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
[CreateAssetMenu(fileName = "Pokemon", menuName = "pkm")]
public class Pokemon : ScriptableObject
{
    public string Base_Pokemon_name;
    public string Pokemon_name;
    public ulong Pokemon_ID = 0;
    public uint Personality_value;
    public string Gender = "None";
    public string GenderRatio = "50/50";
    public Nature nature;
    public bool has_gender = true;
    public float HP;
    public float max_HP;
    public float BaseHP;
    public float BaseAttack;
    public float BaseDefense;
    public float BaseSP_ATK;
    public float BaseSP_DEF;
    public float Basespeed;
    public float Attack;
    public float Defense;
    public float SP_ATK;
    public float SP_DEF;
    public float speed;
    public float HP_IV;
    public float Attack_IV;
    public float Defense_IV;
    public float SP_ATK_IV;
    public float SP_DEF_IV;
    public float speed_IV;
    public float HP_EV=0;
    public float Attack_EV=0;
    public float Defense_EV=0;
    public float SP_ATK_EV=0;
    public float SP_DEF_EV=0;
    public float speed_EV=0;
    public List<string> EVs=new();
    public float Accuracy = 100;
    public float Evasion = 100;
    public float crit_chance = 6.25f;
    public int Current_level = 1;
    public int CurrentExpAmount = 0;
    public float NextLvExpAmount = 0;
    public string EXPGroup = "";
    public int exp_yield=0;
    public bool has_trainer=false;
    public bool canAttack = true;
    public bool isFlinched = false;
    public bool CanBeDamaged = true;
    public List<Type> types;
    public string Status_effect = "None";
    public List<Buff_Debuff> Buff_Debuffs = new();
    public string type_Immunity = "None";
    public string[] evo_line;
    public string[] abilities;
    public bool split_evolution = false;
    public string[] learnSet;
    public List<Move> move_set=new();
    public Ability ability;
    public List<Evolution> evolutions;
    public Item HeldItem;
    public Sprite front_picture;
    public Sprite back_picture;

    //data conversion when json to obj
    public string ability_name;
    public string natureName;
    public List<string> evo_data=new();
    public List<string> type_data=new();
    public List<string> move_data=new();
    public List<int> move_pp_data=new();

    public event Action OnLevelUP;
    public void Set_class_data()
    {
        ability_name = ability.ability;
        natureName = nature.natureName;
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
        nature = Resources.Load<Nature>("Pokemon_project_assets/Pokemon_obj/Natures/" + natureName.ToLower());
        ability = Resources.Load<Ability>("Pokemon_project_assets/Pokemon_obj/Abilities/" + ability_name.ToLower());
        move_set.Clear();
        types.Clear();
        evolutions.Clear();
        for (int i = 0; i < move_data.Count; i++)
        {
            int pos = move_data[i].IndexOf('/')+1;
            string name_ = move_data[i].Substring(0, pos - 1).ToLower();
            string type = move_data[i].Substring(pos,move_data[i].Length - pos).ToLower();
            Move move_copy = Obj_Instance.set_move(Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + type + "/" + name_));
            move_copy.Powerpoints = move_pp_data[i];
            move_set.Add(move_copy);
        }
        foreach (String t in type_data)
            types.Add(Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/" + t.ToLower()));
        foreach (String e in evo_data)
             evolutions.Add(Resources.Load<Evolution>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + Base_Pokemon_name + "/" +e));
        for(int i =0; i < types.Count; i++)
            types[i].type_img = Resources.Load<Sprite>("Pokemon_project_assets/ui/" + type_data[i].ToLower());
    }
    void Check_evolution(int evo_index)
    {
        for (int i = 0; i < evo_line.Length; i++)
        {
            int lv = int.Parse(evo_line[i]);
            if (Current_level == lv)
                Evolve(evolutions[i+evo_index]);
        }
    }
    void split_evo()
    {
        int evo = (int)Personality_value % 10;
        if (evo>=0 & evo<5)
            Check_evolution(0);
        else if (evo>4 & evo<10)
            Check_evolution(2);
    }
    public void Recieve_exp(int amount)
    {
        CurrentExpAmount += amount;
        Debug.Log(Pokemon_name+" recieved "+ amount +" exp current exp: "+CurrentExpAmount);
        Debug.Log("next lv exp: "+PokemonOperations.GetNextLv(this));
        NextLvExpAmount = PokemonOperations.GetNextLv(this);
        if(CurrentExpAmount>=PokemonOperations.GetNextLv(this))
            Level_up();
    }
    public int Calc_Exp(Pokemon enemy)
    {
        float trainer_bonus = 1;
        float BaseExp = (enemy.exp_yield*enemy.Current_level) / 7f;
        float Exp_item_bonus = 1f;
        if (HeldItem!=null)
            if (HeldItem.Item_type == "Exp Gain")
                Exp_item_bonus = float.Parse(HeldItem.Item_effect);
        if (enemy.has_trainer)
            trainer_bonus = 1.5f;
        return (int)math.trunc(BaseExp * trainer_bonus * Exp_item_bonus);
    }
    void Evolve(Evolution evo)
    {
        Pokemon_name = evo.Evo_name;
        EVs=evo.EVs;
        types = evo.types;
        ability = evo.ability;
        learnSet = evo.learnSet;
        front_picture = evo.front_picture;
        back_picture = evo.back_picture;
        exp_yield = evo.exp_yield;
        BaseHP=evo.BaseHP;
        BaseAttack=evo.BaseAttack;
        BaseDefense=evo.BaseDefense;
        BaseSP_ATK=evo.BaseSP_ATK;
        BaseSP_DEF=evo.BaseSP_DEF;
        Basespeed = evo.Basespeed;
    }

    void Increase_Stats()
    {
        Attack = Stat_Increase(BaseAttack,Attack_IV,Attack_EV,"Attack");
        Defense = Stat_Increase(BaseDefense,Defense_IV,Defense_EV,"Defense");
        speed = Stat_Increase(Basespeed,speed_IV,speed_EV,"Speed");
        SP_ATK = Stat_Increase(BaseSP_ATK,SP_ATK_IV,SP_ATK_EV,"Special Attack");
        SP_DEF = Stat_Increase(BaseSP_DEF,SP_DEF_IV,SP_DEF_EV,"Special Defense");
        max_HP = increase_HP();
    }
    float Get_nature_Modifier(string stat)
     {
         if (nature.StatIncrease == stat)
             return 1.1f;
         if (nature.StatDecrease == stat)
             return 0.9f;
         return 1;
     }
    float Stat_Increase(float baseStat,float IV,float EV,string stat)
    {
        return math.round(((((baseStat*IV*(EV/4) * 2)/100)*Current_level)+Current_level+5)*Get_nature_Modifier(stat));
    }
    float increase_HP()
    {
        return math.round((((BaseHP*HP_IV*(HP_EV/4) * 2)/100)*Current_level)+Current_level+10);
    }
    public void Level_up()
    {
        OnLevelUP?.Invoke(); 
        Current_level++;
        NextLvExpAmount = PokemonOperations.GetNextLv(this);
        Increase_Stats();
        if(split_evolution)
            split_evo();
        else
            Check_evolution(0);
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
                    //Debug.Log("moves full");
                else
                    move_set.Add(Obj_Instance.set_move(Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + t + "/" + n)));
                break;
            }
        }
    }
}
