using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public struct MoveBuffData
{
    [SerializeField]public bool isIncreasing;
    public Stat stat;
    [SerializeField]public int amount;
}
