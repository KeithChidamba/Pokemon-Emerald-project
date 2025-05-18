using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Player_Info_ui : MonoBehaviour
{
    public Text playerName;
    public Text playerMoney;
    public Text trainerID;
    public bool viewingProfile;
    public void LoadProfile(Player_data player)
    {
        viewingProfile = true;
        trainerID.text = "ID: "+player.trainerID;
        playerName.text = player.playerName;
        playerMoney.text = player.playerMoney.ToString();
    }
}
