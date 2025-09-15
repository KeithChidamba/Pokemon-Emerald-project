using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Overworld_interactable : MonoBehaviour
{
    public Interaction interaction;
    public string location;
    public enum InteractionType{None,Clerk,PlantBerry,PickBerry,WaterBerryTree}
    public InteractionType interactionType;
    public Encounter_Area area;
}
