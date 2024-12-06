using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_handler : MonoBehaviour
{
    public Options_manager options;
    public Battle_handler battle;
    public Battle_Participant Participant;

    public string removeSpace(string name_)
    {
        char splitter = ' ';
        int space_count = 0;
        List<int> num_spaces = new();
        for (int i = 0; i < name_.Length; i++)
        {
            if (name_[i] == splitter)
            {
                num_spaces.Add(i);
                space_count++;
            }
        }
        string result = "";
        if (space_count > 0)
        {
            int index = 0;
            for (int i = 0; i < space_count; i++)
            {
                result += name_.Substring(index,(num_spaces[i]-index));
                index = num_spaces[i]+1;
            }
            //part after last space
            result+=name_.Substring(num_spaces[space_count - 1]+1, (name_.Length - num_spaces[space_count - 1]-1));
        }
        else
        {
            result = name_;
        }
        return result;
    }
    public void Use_Item(Item item)
    {
        item.quantity--;
        string n = removeSpace(item.Item_name.ToLower());
        Invoke(n,0f);
        Debug.Log(item.Item_name);
    }

    void potion()
    {
        heal(5);
    } 
    void superpotion()
    {
        heal(15);
    }
    void hyperpotion()
    {
        heal(30);
    }
    void oranberry()
    {
        heal(3);
    }

    void burnheal()
    {
        heal_status("burn");
    }
    void antidote()
    {
        heal_status("poison");
    }
    void paralyzheal()
    {
        heal_status("paralysis");
    }

    void pokeball()
    {
        if (battle.is_trainer_battle)
        {
            options.dialogue.Write_Info("Cant catch someone else's Pokemon!","Details");
        }
        else
        {
            //write catch logic later
            //has trainer after catch
        }
    }
    void heal_status(string status)
    {
        if (Participant.pokemon.Status_effect == status)
        {
            Participant.pokemon.Status_effect = "";
            options.dialogue.Write_Info("Pokemon has been healed","Details");
            Invoke(nameof(options.close_bag),1f);
        }
        else if (Participant.pokemon.Status_effect == "None")
        {
            options.dialogue.Write_Info("Pokemon is healthy","Details");
        }
        else
        {
            options.dialogue.Write_Info("Incorrect heal item","Details");
        }
        options.dialogue.Dialouge_off(1f);
}
    void heal(int heal_effect)
    {
        if( (Participant.pokemon.HP + heal_effect) <= Participant.pokemon.max_HP)
            Participant.pokemon.HP += heal_effect;
        else
        {
            options.dialogue.Write_Info("Pokemon health is full","Details");
        }
    }
}
