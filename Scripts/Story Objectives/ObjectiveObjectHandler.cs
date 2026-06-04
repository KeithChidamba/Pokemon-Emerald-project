using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum propState
{
    InActive,InAccessible
}
[Serializable]
public class propStateAfterObjective
{
    public propStateAfterObjective(GameObject propObject, propState propState)
    {
        this.propObject = propObject;
        this.propState = propState;
    }

    public GameObject propObject;
    public propState propState;
}
[Serializable]
public class propStateGroup
{
    public propStateGroup(List<propStateAfterObjective> propsForObjective)
    {
        this.propsForObjective = propsForObjective;
    }
    public List<propStateAfterObjective> propsForObjective;
}
public class ObjectiveObjectHandler : MonoBehaviour,IInjectable
{
  public PropBasedObjective objective;
  public List<propStateGroup> propGroupsForObjective;
  public LayerMask newLayer;
  
  private OverworldState _overworldStateHandler;

  public void Inject(ServiceContainer container)
  {
      _overworldStateHandler = container.Resolve<OverworldState>();
      gameObject.SetActive(true);
  }

  public void OnInject()
  {
      _overworldStateHandler.OnObjectivesLoaded += CheckForRequiredObjective;
  }

    private void CheckForRequiredObjective()
    {
        if(objective.requiresPickupItems)
        {
            _overworldStateHandler.OnPickupItemCreated += objective.ReceivePickupObjects;
        }
        if (_overworldStateHandler.HasObjective(objective.name))//the objective could have been completed already
        {
            objective.Inject(this);
            objective.OnLoad += LoadObjects;
            objective.OnClear += UnLoadObjects;
        }
    }
    private void LoadObjects()
    {
        foreach (var group in propGroupsForObjective)
        {
            foreach (var prop in group.propsForObjective)
            {
                prop.propObject.SetActive(true);
            }
        }
    }
    private void UnLoadObjects()
    {
        foreach (var group in propGroupsForObjective)
        {
            foreach (var prop in group.propsForObjective)
            {
                switch (prop.propState)
                {
                    case propState.InActive:
                        prop.propObject.SetActive(false);
                        break;
                    case propState.InAccessible:
                        prop.propObject.layer = newLayer;
                        break;
                    default:
                        prop.propObject.SetActive(false);
                        break;
                }
            }
        }
        objective.OnLoad -= LoadObjects;
        objective.OnClear -= UnLoadObjects;
    }
}
