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
    public GameObject propObject;
    public propState propState;
}
public class ObjectiveObjectHandler : MonoBehaviour,IInjectable
{
  public StoryObjective objective;
  public StoryObjectiveType objectiveType;
  public List<propStateAfterObjective> propsForObjective;
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
            if (objectiveType == StoryObjectiveType.GiftPokemon)
            {
                var pokemonObjective = (GiftPokemonObjective)objective;
                pokemonObjective.objectiveObjectHandler = this;
            }
            
            objective.OnLoad += LoadObjects;
            objective.OnClear += UnLoadObjects;
        }
    }
    private void LoadObjects()
    {
        propsForObjective.ForEach(prop=>prop.propObject.SetActive(true));
    }
    private void UnLoadObjects()
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
            }
        }
       
        objective.OnLoad -= LoadObjects;
        objective.OnClear -= UnLoadObjects;
    }

}
