using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "player", menuName = "pl_dt")]
public class Player_data : ScriptableObject
{
    public string Player_name;
    public ushort  Trainer_ID;
    public ushort  Secret_ID;
    public int player_Money;
    public Vector3 player_Position;
    public string Location;

    public ushort Generate_ID()
    {
        System.Random rand = new System.Random();
        ushort  random32bit = (ushort)rand.Next(0, 65536);
        return random32bit;
    }
}
