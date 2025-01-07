using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player_Info_ui : MonoBehaviour
{
    public Text player_name;
    public Text player_money;
    public Text TrainerID;
    public bool Viewing_profile = false;
    public void Load_Profile(Player_data player)
    {
        Viewing_profile = true;
        TrainerID.text = "ID: "+player.Trainer_ID.ToString();
        player_name.text = player.Player_name;
        player_money.text = player.player_Money.ToString();
    }
}
