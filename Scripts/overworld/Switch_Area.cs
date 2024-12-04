using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch_Area : MonoBehaviour
{
    public GameObject overworld;
    public GameObject interior;
    public string area_name ="";
    public bool exiting_area = false;
    public bool inside_area = false;
    public bool has_animation=false;
    public Animator door;
    public Transform Mat_pos;
    public Transform door_pos;
}
