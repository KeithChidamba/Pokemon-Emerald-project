using System;
using UnityEngine.Serialization;

[Serializable]
public struct MoveSaveData
{
    public string moveName;
    public int powerPoints;
    public int maxPowerpoints;
    public MoveSaveData(string name, int powerPoints, int maxPowerpoints)
    {
        moveName = name;
        this.powerPoints = powerPoints;
        this.maxPowerpoints = maxPowerpoints;
    }
}
