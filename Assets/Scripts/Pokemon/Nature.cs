using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Nature", menuName = "Pokemon/Nature")]
public class Nature : ScriptableObject
{
    public string natureName;
    public Stat statToIncrease;
    public Stat statToDecrease;
    [FormerlySerializedAs("PValue")] public int requiredNatureValue;
}
