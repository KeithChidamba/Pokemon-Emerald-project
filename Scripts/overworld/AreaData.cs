using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "area", menuName = "Overworld/areaData")]
public class AreaData : ScriptableObject
{
    public AreaName areaName;
    public bool escapable;
}

public enum AreaName
{
    OverWorld,PlayerGarden,PokeMartCoastal,PokeCenter
}
