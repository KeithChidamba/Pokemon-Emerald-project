using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Switch_Area : MonoBehaviour
{
    public GameObject overworld;
    public GameObject interior;
    [FormerlySerializedAs("area_name")] public string areaName ="";
    
    [FormerlySerializedAs("exiting_area")] public bool exitingArea = false;
    [FormerlySerializedAs("inside_area")] public bool insideArea = false;
    [FormerlySerializedAs("has_animation")] public bool hasDoorAnimation=false;
    public bool escapable = false;
    [FormerlySerializedAs("door")] public Animator doorAnimation;
    [FormerlySerializedAs("Mat_pos")] public Transform doormatPosition;
    [FormerlySerializedAs("door_pos")] public Transform doorPosition;
}
