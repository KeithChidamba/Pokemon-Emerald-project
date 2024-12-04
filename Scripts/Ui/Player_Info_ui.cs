using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player_Info_ui : MonoBehaviour
{
    public Text player_name;
    public Text player_money;
    public bool Viewing_profile = false;
    void Update()
    {
        
    }

    public void Load_Profile(string player_n,int player_m)
    {
        Viewing_profile = true;
        player_name.text = player_n;
        player_money.text = player_m.ToString();
    }
}
