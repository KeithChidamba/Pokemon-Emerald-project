using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "player", menuName = "pl_dt")]
public class Player_data : ScriptableObject
{
    public string Player_name;
    public int  Trainer_ID;
    public int  Secret_ID;
    public int player_Money;
    public Vector3 player_Position;
    public string Location;
}
