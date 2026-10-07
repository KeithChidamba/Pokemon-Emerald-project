using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PlayerProfileUI
{
    public Text playerName;
    public Text playerMoney;
    public Text trainerID;
    public GameObject parentObject;
    public void LoadProfile(PlayerData player)
    {
        trainerID.text = "ID: "+player.trainerID;
        playerName.text = player.playerName;
        playerMoney.text = player.playerMoney.ToString();
    }
}
