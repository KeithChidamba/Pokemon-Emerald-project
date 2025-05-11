using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "player", menuName = "pl_dt")]
public class Player_data : ScriptableObject
{
    [FormerlySerializedAs("Player_name")] public string playerName;
    [FormerlySerializedAs("Trainer_ID")] public int  trainerID;
    [FormerlySerializedAs("Secret_ID")] public int  secretID;
    [FormerlySerializedAs("player_Money")] public int playerMoney;
    [FormerlySerializedAs("NumBadges")] public int numBadges = 0;
    [FormerlySerializedAs("player_Position")] public Vector3 playerPosition;
    [FormerlySerializedAs("Location")] public string location;
}
