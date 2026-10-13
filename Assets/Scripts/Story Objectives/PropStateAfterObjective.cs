using System;
using UnityEngine;

public enum propState
{
    InActive,InAccessible,Destroyed
}
[Serializable]
public class PropStateAfterObjective
{
    public Vector3 propPosition;
    [HideInInspector]public GameObject propObject;
    [HideInInspector]public propState propState;
}