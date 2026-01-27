using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveObjectHandler : MonoBehaviour
{
  public StoryObjective objective;
  public List<GameObject> propsForObjective;
  public bool removeOnObjectiveClear;
    void Start()
    {
        OverworldState.Instance.OnObjectivesLoaded += CheckForRequiredObjective;
    }

    private void CheckForRequiredObjective()
    {
        if (OverworldState.Instance.HasObjective(objective.name))
        {
            objective.OnLoad += LoadObjects;
            if(removeOnObjectiveClear) objective.OnClear += UnLoadObjects;
        }
        else
        {
            UnLoadObjects();
        }
    }
    private void LoadObjects()
    {
        propsForObjective.ForEach(prop=>prop.SetActive(true));
    }
    private void UnLoadObjects()
    {
        propsForObjective.ForEach(prop=>prop.SetActive(false));
        objective.OnLoad -= LoadObjects;
        objective.OnClear -= UnLoadObjects;
    }
}
