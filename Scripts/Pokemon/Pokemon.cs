using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Pokemon", menuName = "pkm")]
public class Pokemon : ScriptableObject
{
    public string Base_Pokemon_name;
    public string Pokemon_name;
    public long Pokemon_ID = 0;
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
    public float CatchRate = 0;
    public int Current_level = 1;
    public int CurrentExpAmount = 0;
    public float NextLevelExpAmount = 0;
    public string EXPGroup = "";
    public int exp_yield=0;
    public bool has_trainer=false;
    public bool canAttack = true;
    public bool isFlinched = false;
    public bool CanBeDamaged = true;
    public List<Type> types;
    public string Status_effect = "None";
    public List<Buff_Debuff> Buff_Debuffs = new();
    public string[] evo_line;
    public bool RequiresEvolutionStone = false;
    public string EvolutionStoneName = "None";
    public string[] abilities;
    public bool split_evolution = false;
    public string[] learnSet;
    public List<Move> move_set=new();
    public Ability ability;
    public List<Evolution> evolutions;
    public Item HeldItem;
    public bool HasItem = false;
    public Sprite front_picture;
    public Sprite back_picture;
    public event Action OnNewLevel;
    public event Action<Pokemon> OnLevelUp;
    //data conversion when json to obj
    public string ability_name;
    public string natureName;
    public List<string> evo_data=new();
    public List<string> type_data=new();
    public List<string> move_data=new();
    public List<string> move_pp_data=new();
    
    public void SaveUnserializableData()
    {
        ability_name = ability.abilityName;
        natureName = nature.natureName;
        HasItem = (HeldItem != null);
        move_data.Clear();
        type_data.Clear();
        move_pp_data.Clear();
        evo_data.Clear();
        foreach (var move in move_set)
        {
            move_data.Add(move.Move_name + "/" + move.type.Type_name);
            move_pp_data.Add(move.Powerpoints+"/"+move.max_Powerpoints);
        }
        foreach (Type t in types)
            type_data.Add(t.Type_name);
        foreach (Evolution e in evolutions)
            evo_data.Add(e.Evo_name);
    }
    public void LoadUnserializedData()//gives values to attributes that cant be deserialized, using saved values
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
            var splitPos1 = move_data[i].IndexOf('/')+1;
            var moveName = move_data[i].Substring(0, splitPos1 - 1).ToLower();
            var moveType = move_data[i].Substring(splitPos1,move_data[i].Length - splitPos1).ToLower();
            var moveCopy = Obj_Instance.set_move(Resources.Load<Move>("Pokemon_project_assets/Pokemon_obj/Moves/" + moveType + "/" + moveName));
            var splitPos2 = move_pp_data[i].IndexOf('/')+1;
            moveCopy.Powerpoints = int.Parse(move_pp_data[i].Substring(0, splitPos2-1));
            moveCopy.max_Powerpoints = int.Parse(move_pp_data[i].Substring(splitPos2, move_pp_data[i].Length - splitPos2));
            move_set.Add(moveCopy);
        }
        foreach (String t in type_data)
            types.Add(Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/" + t.ToLower()));
        foreach (String e in evo_data)
             evolutions.Add(Resources.Load<Evolution>("Pokemon_project_assets/Pokemon_obj/Pokemon/" + Base_Pokemon_name + "/" +e));
        for(int i =0; i < types.Count; i++)
            types[i].type_img = Resources.Load<Sprite>("Pokemon_project_assets/ui/" + type_data[i].ToLower());
        Battle_handler.Instance.OnBattleEnd += ClearEvents;
    }

    public void RemoveHeldItem()
    {
        HeldItem = null;
        HasItem = false;
    }

    public void GiveItem(Item itemToGive)
    {
        HeldItem = Obj_Instance.set_Item(itemToGive);
        HeldItem.quantity = 1;
        HasItem = true;
    }
    public bool HasType(string typeName)
    {
        Type type = Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/"+typeName.ToLower());
        foreach (Type t in types)
            if (t == type)
                return true;
        return false;
    }
    public void CheckEvolutionRequirements(int evoIndex)
    {
        if (RequiresEvolutionStone)
        { Evolve(evolutions[evoIndex]); return; }
        
        for (int i = 0; i < evo_line.Length; i++)
        {
            var requiredLevelToEvolve = int.Parse(evo_line[i]);
            if (Current_level == requiredLevelToEvolve)
            {
                Evolve(evolutions[i+evoIndex]);
                break;
            }
        }
    }
    void DetermineEvolution()
    {
        int evolutionValue = (int)Personality_value % 10;
        if (evolutionValue>=0 & evolutionValue<5)
            CheckEvolutionRequirements(0);
        else if (evolutionValue>4 & evolutionValue<10)
            CheckEvolutionRequirements(2);
    }
    public void ReceiveExperience(int amount)
    {
        if (Current_level > 99) return;
        CurrentExpAmount += amount;
        NextLevelExpAmount = PokemonOperations.GetNextLv(Current_level,EXPGroup);
        if(CurrentExpAmount>NextLevelExpAmount)
            LevelUp();
    }
    public int CalculateExperience(Pokemon enemy)
    {
        var trainerBonus = 1f;
        var baseExp = (enemy.exp_yield*enemy.Current_level) / 7f;
        var expItemBonus = 1f;
        if (HeldItem!=null)
            if (HeldItem.itemType == "Exp Gain")//lucky egg
                expItemBonus = float.Parse(HeldItem.itemEffect);
        if (enemy.has_trainer)
            trainerBonus = 1.5f;
        return (int)math.trunc(baseExp * trainerBonus * expItemBonus);
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
        EXPGroup = evo.EXPGroup;
        exp_yield = evo.exp_yield;
        CatchRate = evo.CatchRate;
        BaseHP=evo.BaseHP;
        BaseAttack=evo.BaseAttack;
        BaseDefense=evo.BaseDefense;
        BaseSP_ATK=evo.BaseSP_ATK;
        BaseSP_DEF=evo.BaseSP_DEF;
        Basespeed = evo.Basespeed;
    }

    void IncreaseStats()
    {
        Attack = DetermineStatIncrease(BaseAttack,Attack_IV,Attack_EV,"Attack");
        Defense = DetermineStatIncrease(BaseDefense,Defense_IV,Defense_EV,"Defense");
        speed = DetermineStatIncrease(Basespeed,speed_IV,speed_EV,"Speed");
        SP_ATK = DetermineStatIncrease(BaseSP_ATK,SP_ATK_IV,SP_ATK_EV,"Special Attack");
        SP_DEF = DetermineStatIncrease(BaseSP_DEF,SP_DEF_IV,SP_DEF_EV,"Special Defense");
        max_HP = DetermineHealthIncrease();
        if (Current_level == 1)
            HP = max_HP;
    }
    float GetNatureModifier(string stat)
     {
         if (nature.StatIncrease == stat)
             return 1.1f;
         if (nature.StatDecrease == stat)
             return 0.9f;
         return 1;
     }
    float DetermineStatIncrease(float baseStat,float IV,float EV,string stat)
    {
        float brackets1 = (2*baseStat) + IV + (EV / 4);
        float bracket2 = brackets1 * (Current_level / 100f);
        float bracket3 = bracket2 + 5f;
        return math.floor(bracket3 * GetNatureModifier(stat));
    }
    float DetermineHealthIncrease()
    {
        float brackets1 = (2*BaseHP) + HP_IV + (HP_EV / 4);
        float bracket2 = brackets1 * (Current_level / 100f);
        float bracket3 = bracket2 + Current_level + 10f;
        return math.floor(bracket3);
    }
    public void LevelUp()
    {
        OnLevelUp?.Invoke(this);
        Current_level++;
        NextLevelExpAmount = PokemonOperations.GetNextLv(Current_level,EXPGroup);
        IncreaseStats();
        if (!RequiresEvolutionStone)
        {
            if(split_evolution)
                DetermineEvolution();
            else
                CheckEvolutionRequirements(0);
        }
        if (!Options_manager.instance.playerInBattle)//artificial level up
            PokemonOperations.GetNewMove(this);
        OnNewLevel?.Invoke();
        while(CurrentExpAmount>NextLevelExpAmount)
            LevelUp();
    }
    private void ClearEvents()
    {
        OnLevelUp = null;
        OnNewLevel = null;
    }
}
