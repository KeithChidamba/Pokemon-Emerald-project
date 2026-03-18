using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveObjectHandler : MonoBehaviour,IInjectable
{
  public StoryObjective objective;
  public List<GameObject> propsForObjective;
  public bool removeOnObjectiveClear;
  public bool changeLayer;
  public LayerMask newLayer;
  private OverworldState _overworldStateHandler;
  public void Inject(Container container)
  {
      _overworldStateHandler = container.Resolve<OverworldState>();
      gameObject.SetActive(true);
      OnInject();
  }

  private void OnInject()
  {
        _overworldStateHandler.OnObjectivesLoaded += CheckForRequiredObjective;
  }

    private void CheckForRequiredObjective()
    {
        if (_overworldStateHandler.HasObjective(objective.name))
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
