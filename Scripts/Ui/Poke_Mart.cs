using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Poke_Mart : MonoBehaviour
{
    public List<Item> mart_items;
    public bool viewing_store = false;
    public Store_Item_ui[] mart_items_ui;

    public int num_items = 0;
    public int Selected_item = 0;
    public int Selected_item_quantity = 0;
    public int top_index = 0;
    public GameObject buy;
    public GameObject mart_ui;
    public GameObject quantity_parent;
    public Text quantity;    
    public Text Player_Money;
    public static Poke_Mart instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    private void Update()
    {
        if(viewing_store)
        {
            quantity.text = Selected_item_quantity.ToString();
            Player_Money.text = Game_Load.instance.player_data.player_Money.ToString();
        }
    }
    public void Select_Item(int Item_pos)
    {
        Selected_item = Item_pos;
        Selected_item_quantity = 1;
        mart_items_ui[Selected_item - 1].Load_item_info();
        buy.SetActive(true);
        quantity_parent.SetActive(true);
    }
    public void Go_Down()
    {
        if (top_index < num_items - 7)
        {
            for (int i = 0; i < 6; i++)
            {
                mart_items_ui[i].item = mart_items_ui[i + 1].item;
            }
            mart_items_ui[6].item = mart_items[top_index + 7];
            Reload_items();
            top_index++;
        }

    }
    public void Go_Up()
    {
        if (top_index > 0)
        {
            for (int i = 6; i > 0; i--)
            {
                mart_items_ui[i].item = mart_items_ui[i - 1].item;
            }
            mart_items_ui[0].item = mart_items[top_index - 1];
            Reload_items();
            top_index--;
        }
    }
    public void Buy()
    {
        if(Game_Load.instance.player_data.player_Money >= mart_items[top_index+Selected_item - 1].price)
        {
            Item item = Obj_Instance.set_Item(mart_items[top_index + Selected_item - 1]);
            item.quantity = Selected_item_quantity;
            Bag.instance.Add_item(item);
            Game_Load.instance.player_data.player_Money -= Selected_item_quantity * item.price;
            Dialogue_handler.instance.Write_Info("You bought "+ item.quantity+ " "+item.Item_name+"'s", "Details");
            Dialogue_handler.instance.Dialouge_off(1.2f);
            Selected_item_quantity = 1;
        }
        else
        {
            Dialogue_handler.instance.Write_Info("You dont have enough money for that!", "Details");
            Dialogue_handler.instance.Dialouge_off(1f);
        }
    }
    public void change_quant(int diff)
    {

        if (diff < 0)//lower quantity
        {
            if (Selected_item_quantity > 1)
            {
                Selected_item_quantity += diff;
            }
            else
            {
                Selected_item_quantity = 1;
            }
        }
        else if(diff > 0)//increase quantity
        {
            if (Selected_item_quantity < 99)//below max quantity and affordable by player
            {
                if(Game_Load.instance.player_data.player_Money >= ( (Selected_item_quantity+1) * mart_items[top_index + Selected_item - 1].price))
                {
                    Selected_item_quantity += diff;
                }  
            }
            else
            {
                Selected_item_quantity = 99;
            }
        }
    }

    public void View_store()
    {
        viewing_store = true;
        Player_Money.text = Game_Load.instance.player_data.player_Money.ToString();
        int num_i = 0;
        num_items = mart_items.Count;
        if (num_items < 8)//if less than amount of ui elements, load that number
            num_i = num_items;
        else //if greater than amount of ui elements just load the first seven
            num_i = 7;
        for (int i = 0; i < num_i; i++)
        {
            mart_items_ui[i].item = mart_items[i];
            mart_items_ui[i].gameObject.SetActive(true);
            mart_items_ui[i].Load_item();
        }
        Select_Item(1);
    }
    public void Exit_Store()
    {
        Selected_item = 0;
        buy.SetActive(false);
        quantity_parent.SetActive(false);
        mart_items_ui[0].Clear_ui();
        viewing_store = false;
    }
    void Reload_items()
    {
        mart_items_ui[0].Clear_ui();
        foreach (Store_Item_ui itm in mart_items_ui)
        {
            itm.Load_item();
        }
        Select_Item(1);
    }
}
