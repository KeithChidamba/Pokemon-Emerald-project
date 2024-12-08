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
    private Item item_in_use;

    public void Use_Item(Item item)
    {
        item_in_use = item;
        switch (item.Item_type.ToLower())
        {
            case "potion":
                Heal(int.Parse(item.Item_effect));
                break;
            case "status":
                heal_status(item.Item_effect.ToLower());
                break;
        }
    }
    void Use_pokeball()
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
    private void heal_status(string status)
    {
        if (selected_party_pkm.Status_effect.ToLower() == status)
        {
            selected_party_pkm.Status_effect = "None";
            options.dialogue.Write_Info("Pokemon has been healed","Details");
            item_in_use.quantity--;
            options.player_bag.check_Quantity(item_in_use);
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
    private void Heal(int heal_effect)
    {
        if ((selected_party_pkm.HP + heal_effect) <= selected_party_pkm.max_HP)
        {
            selected_party_pkm.HP += heal_effect;
            options.dialogue.Write_Info(selected_party_pkm.Pokemon_name+" gained "+heal_effect+" health points","Details");
            item_in_use.quantity--;
            options.player_bag.check_Quantity(item_in_use);
        }
        else
        {
            options.dialogue.Write_Info("Pokemon health is full","Details");
        }
        options.dialogue.Dialouge_off(1f);
        Using_item = false;
    }
}
