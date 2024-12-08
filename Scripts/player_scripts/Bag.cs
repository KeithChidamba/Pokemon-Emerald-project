using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Mathematics;
using UnityEngine.UI;

public class Bag : MonoBehaviour
{
    public List<Item> bag_items;
    public bool viewing_bag = false;
    public Item_ui[] bag_items_ui;
    public int max_capacity = 50;
    public int num_items = 0;
    public int Selected_item = 0;
    public int top_index = 0;//keeps track of visible bag items
    public Options_manager options;
    public GameObject[] item_actions;
    public int Sell_quantity = 1;
    public bool Selling_items = false;
    public GameObject Selling_ui;
    public Text Sell_qty_txt;
    public Game_ui_manager ui_m;
    private void Update()
    {
        if (Selling_items)
        {
           Sell_qty_txt.text = "X"+Sell_quantity.ToString();
        }
    }

    public void Select_Item(int Item_pos)
    {
        Selected_item = Item_pos;
        bag_items_ui[Selected_item - 1].Load_item_info();
        if (Selling_items)
        {
            Sell_quantity = 1;
            Selling_ui.SetActive(true);
        }
        else
        {
            foreach (GameObject i in item_actions)
            {
                i.SetActive(true);
            }
        }
    }
    
    public void Sell_to_Market()
    {
        int price = bag_items[top_index + Selected_item - 1].price;
        int profit = (int)math.trunc((Sell_quantity * price)/2);
        options.player_data.player_Money += profit;
        bag_items[top_index + Selected_item - 1].quantity -= Sell_quantity;
        if (bag_items[top_index + Selected_item - 1].quantity == 0)
        {
            Remove_item();
        }
        options.dialogue.Write_Info("You made P"+profit.ToString()+ ", would you like to sell anything else?", "Options", "Sell_item","Sure, which item?","Dont_Buy","Yes","No");
        ui_m.close_bag();
    }

    public void check_Quantity(Item item)
    {
        if (item.quantity < 1)
        {
            bag_items.Remove(item);
        }
    }
    public void change_quant(int diff)
    {
        if (diff < 0)//lower quantity
        {
            if (Sell_quantity > 1)
            {
                Sell_quantity += diff;
            }
            else
            {
                Sell_quantity = 1;
            }
        }
        else if(diff > 0)//increase quantity
        {
            if (Sell_quantity<bag_items[top_index+Selected_item-1].quantity)//below max available item quantity
            {
                Sell_quantity += diff;
            }
            else
            {
                Sell_quantity=bag_items[top_index+Selected_item-1].quantity;
            }
        }
    }
    public void Go_Down()
    {
        if (top_index < num_items-10)
        {
            for (int i = 0; i < 9; i++)
            {
                bag_items_ui[i].item = bag_items_ui[i + 1].item;  
            }
            bag_items_ui[9].item = bag_items[top_index + 10];
            Reload_items();
            top_index++;
        }

    }
    public void Go_Up()
    {
        if (top_index > 0)
        {
            for (int i = 9; i > 0; i--)
            {
                bag_items_ui[i].item = bag_items_ui[i-1].item;
            }
            bag_items_ui[0].item = bag_items[top_index - 1];
            Reload_items();
            top_index--;
        }
    }
    Item Search_items(string item)
    {
        Item item_ = null;
        foreach (Item itm in bag_items)
        {
            if (itm.Item_name == item )
            {
                if(itm.quantity < 99)
                {
                    item_ = itm;
                }         
            }
        }
        return item_;
    }
    bool inBag(string item)
    {
        foreach (Item itm in bag_items)
        {
            if (itm.Item_name == item)
            {
                return true;
            }
        }
        return false;
    }
    public void Remove_item()
    {
        bag_items.Remove(bag_items[top_index + Selected_item - 1]);
        foreach (Item_ui i in bag_items_ui)
        {
            i.gameObject.SetActive(false);
        }
        bag_items_ui[0].Clear_ui();
        foreach (GameObject i in item_actions)
        {
            i.SetActive(false);
        }
        View_bag();
    }
    public void Add_item(Item item)
    {
        if (num_items < max_capacity)
        {
            if (inBag(item.Item_name))
            {
                Item searched = Search_items(item.Item_name);
                if (searched != null)
                {
                    if ( item.quantity < (99 - searched.quantity))
                    {
                        searched.quantity += item.quantity;
                    }
                    else
                    {
                        int quantity_gap = (99 - searched.quantity);
                        searched.quantity += quantity_gap;
                        Item overflow = options.ins_manager.set_Item(item);
                        overflow.quantity = item.quantity - quantity_gap;
                        bag_items.Add(overflow);
                        num_items++;
                    }
                }
                else
                {
                    bag_items.Add(options.ins_manager.set_Item(item));
                    num_items++;
                }
            }
            else
            {
                bag_items.Add(options.ins_manager.set_Item(item));
                num_items++;
            }
        }
        else
        {
            ui_m.Close_Store();
            options.dialogue.Write_Info("Bag is full", "Details");
        }                                                                           
    }
    public void use_item()
    {
        options.item_h.Using_item = true;
        options.party.Recieve_item(bag_items[top_index + Selected_item - 1]);
        ui_m.close_bag();
        ui_m.View_pkm_Party();
    }
    public void Close_bag()
    {
        Selected_item = 0;
        Selling_ui.SetActive(false);
        Selling_items = false;
        foreach (GameObject i in item_actions)
        {
            i.SetActive(false);
        }
        for (int i = 0; i < 10; i++)
        {
            bag_items_ui[i].gameObject.SetActive(false);
        }
        bag_items_ui[0].Clear_ui();
    }
    public void View_bag()
    {
        num_items = 0;
        viewing_bag = true;
        int num_i = 0;
        foreach (Item item in bag_items)
        {
            if (item != null)
            {
                num_items++;
            }
        }
        if (num_items < 11)
        {
            num_i = num_items;
        }
        else
        {
            num_i = 10;
        }
        for (int i = 0; i < num_i; i++)
        {
            bag_items_ui[i].item = bag_items[i];
            bag_items_ui[i].gameObject.SetActive(true);
            bag_items_ui[i].Load_item();
        }
    }
    void Reload_items()
    {
        bag_items_ui[0].Clear_ui();
        foreach (Item_ui itm in bag_items_ui)
        {
            itm.Load_item();
        }
    }
}

