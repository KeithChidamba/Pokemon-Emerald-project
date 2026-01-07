using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Overworld_interactable : MonoBehaviour
{
    public Interaction interaction;
    public AreaName location;
    public OverworldInteractionType overworldInteractionType;
    public Encounter_Area area;
}

public enum OverworldInteractionType
{
    None,Clerk,PlantBerry,PickBerry,WaterBerryTree,PokemonCenter,Battle
}
