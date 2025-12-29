using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "area", menuName = "areaData")]
public class AreaData : ScriptableObject
{
    public AreaName areaName;
    public bool exitingArea;
    public bool insideArea;
    public bool hasDoorAnimation;
    public bool escapable;
}

public enum AreaName
{
    OverWorld,PlayerHouse,PokeMartForest,PokeMartCoastal,PokeCenter,Museum
}
