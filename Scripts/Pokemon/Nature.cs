using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Nature", menuName = "ntr")]
public class Nature : ScriptableObject
{
    public string natureName;
    [FormerlySerializedAs("StatIncrease")] public string statIncrease;
    [FormerlySerializedAs("StatDecrease")] public string statDecrease;
    [FormerlySerializedAs("PValue")] public int requiredNatureValue;
}
