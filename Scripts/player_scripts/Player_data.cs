using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "player", menuName = "pl_dt")]
public class Player_data : ScriptableObject
{
    public string playerName;
    public ushort  trainerID;
    public ushort  secretID;
    public int playerMoney;
    public int numBadges = 0;
    public Vector3 playerPosition;
    public string location;
    public string equippedItemName;
}
