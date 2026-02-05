using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveObjectHandler : MonoBehaviour
{
  public StoryObjective objective;
  public List<GameObject> propsForObjective;
  public bool removeOnObjectiveClear;
  public bool changeLayer;
  public LayerMask newLayer;
  void Start()
    {
        OverworldState.Instance.OnObjectivesLoaded += CheckForRequiredObjective;
    }

    private void CheckForRequiredObjective()
    {
        if (OverworldState.Instance.HasObjective(objective.name))
        {
            objective.OnLoad += LoadObjects;
            objective.OnClear += UnLoadObjects;
        }
    }
    private void LoadObjects()
    {
        propsForObjective.ForEach(prop=>prop.SetActive(true));
    }
    private void UnLoadObjects()
    {
        if (changeLayer)
        {
            Debug.Log("changed");
            propsForObjective.ForEach(prop=>prop.layer=newLayer);
        }
        if(removeOnObjectiveClear)
        {
            propsForObjective.ForEach(prop=>prop.SetActive(false));
        }
       
        objective.OnLoad -= LoadObjects;
        objective.OnClear -= UnLoadObjects;
    }
}
