using System.Collections.Generic;
using UnityEngine;

public abstract class PropBasedObjective : StoryObjective
{
    public List<PropStateAfterObjective> propsForObjective = new();
    public GameObject propPrefab;
    public LayerMask newLayer;

    protected virtual void OnPropsCreated() { }
    
    protected void LoadObjects(OverworldState overworldStateHandler)
    {
        foreach (var prop in propsForObjective)
        { 
            prop.propObject 
                = Instantiate(propPrefab, prop.propPosition, 
                    propPrefab.transform.rotation,overworldStateHandler.overworldPickupParent);
            prop.propObject.SetActive(true);
        }
        OnPropsCreated();
    }
    
    protected void UnLoadObjects()
    {
        foreach (var prop in propsForObjective)
        {
            switch (prop.propState)
            {
                case propState.InActive:
                    prop.propObject.SetActive(false);
                    break;
                case propState.InAccessible:
                    prop.propObject.layer = newLayer;
                    break;
                case propState.Destroyed:
                    Destroy(prop.propObject);
                    break;
                default:
                    prop.propObject.SetActive(false);
                    break;
            }
        }
    }
}
