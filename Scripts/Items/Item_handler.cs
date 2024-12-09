using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_handler : MonoBehaviour
{
    public Pokemon selected_party_pkm;
    public bool Using_item = false;
    private Item item_in_use;
    public static Item_handler instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
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
        if (Battle_handler.instance.is_trainer_battle)
        {
            Dialogue_handler.instance.Write_Info("Cant catch someone else's Pokemon!","Details");
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
            Dialogue_handler.instance.Write_Info("Pokemon has been healed","Details");
            item_in_use.quantity--;
            Bag.instance.check_Quantity(item_in_use);
        }
        else if (selected_party_pkm.Status_effect == "None")
        {
            Dialogue_handler.instance.Write_Info("Pokemon is healthy","Details");
        }
        else
        {
            Dialogue_handler.instance.Write_Info("Incorrect heal item","Details");
        }
        Dialogue_handler.instance.Dialouge_off(1f);
        Using_item = false;
        Pokemon_party.instance.Refresh_Member_Cards();
}
    private void Heal(int heal_effect)
    {
        if ((selected_party_pkm.HP + heal_effect) <= selected_party_pkm.max_HP)
        {
            selected_party_pkm.HP += heal_effect;
            Dialogue_handler.instance.Write_Info(selected_party_pkm.Pokemon_name+" gained "+heal_effect+" health points","Details");
            item_in_use.quantity--;
            Bag.instance.check_Quantity(item_in_use);
        }
        else
        {
            Dialogue_handler.instance.Write_Info("Pokemon health is full","Details");
        }
        Dialogue_handler.instance.Dialouge_off(1f);
        Using_item = false;
    }
}
