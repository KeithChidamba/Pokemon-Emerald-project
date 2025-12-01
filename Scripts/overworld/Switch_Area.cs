using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Switch_Area : MonoBehaviour
{
    public GameObject overworld;
    public GameObject interior;
    public AreaData areaData;
    [FormerlySerializedAs("door")] public Animator doorAnimation;
    [FormerlySerializedAs("Mat_pos")] public Transform doormatPosition;
    [FormerlySerializedAs("door_pos")] public Transform doorPosition;
}
