using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Item", menuName = "itm")]
public class Item : ScriptableObject
{
    public string Item_ID = "";
    public string Item_name = "";
    public string Item_type = "";
    public string Item_effect = "";
    public string Item_desc = "";
    public int price = 0;
    public int quantity = 0;
    public Sprite Item_img;
    public void Set_img()
    {
        Item_img = Resources.Load<Sprite>("Pokemon_project_assets/ui/" + Item_name.ToLower());
    }
}
