using UnityEngine;

[CreateAssetMenu(fileName = "player", menuName = "Player Data")]
public class Player_data : ScriptableObject
{
    public string playerName;
    public ushort  trainerID;
    public ushort  secretID;
    public int playerMoney;
    public int numBadges = 0;
    public Vector3 playerPosition;
    public AreaName location;
    public string equippedItemName;
}
