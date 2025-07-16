using System;

[Serializable]
public struct MoveSaveData
{
    public string moveName;
    public int powerPoints;
    public int maxPowerPoints;
    public string moveType; //because this is just save data, no need for enum because no risk of typos or bugs

    public MoveSaveData(string name, string type, int powerPoints, int maxPowerPoints)
    {
        moveName = name;
        moveType = type;
        this.powerPoints = powerPoints;
        this.maxPowerPoints = maxPowerPoints;
    }
}
