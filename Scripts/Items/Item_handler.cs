using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_handler : MonoBehaviour
{
    public Options_manager options;
    public Battle_handler battle;
    public Pokemon selected_party_pkm;
    public bool Using_item = false;
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
        if (selected_party_pkm.Status_effect.ToLower() == status)
        {
            selected_party_pkm.Status_effect = "";
            options.dialogue.Write_Info("Pokemon has been healed","Details");
        }
        else if (selected_party_pkm.Status_effect == "None")
        {
            options.dialogue.Write_Info("Pokemon is healthy","Details");
        }
        else
        {
            options.dialogue.Write_Info("Incorrect heal item","Details");
        }
        options.dialogue.Dialouge_off(1f);
        Using_item = false;
        options.party.Refresh_Member_Cards();
}
    void heal(int heal_effect)
    {
        if ((selected_party_pkm.HP + heal_effect) <= selected_party_pkm.max_HP)
        {
            selected_party_pkm.HP += heal_effect;
            options.dialogue.Write_Info(selected_party_pkm.Pokemon_name+" gained "+heal_effect.ToString()+" health points","Details");
        }
        else
        {
            options.dialogue.Write_Info("Pokemon health is full","Details");
        }
        options.dialogue.Dialouge_off(1f);
        Using_item = false;
    }
}
