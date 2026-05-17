using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AreaData
{
    public AreaTransitionData data;
    public Vector3 tileLocation;
    public List<GameObject> npcList;
    [SerializeField]private List<NpcLogic> npcLogicScripts = new();
    
    public void LoadNpcObjects()
    {
        if(npcList.Count == 0)
        {
            return;
        }
        
        foreach (var npc in npcList)
        {
            npc.SetActive(true);
            var npcLogic = npc.GetComponentInChildren<NpcLogic>();
            npcLogicScripts.Add(npcLogic);
        }
    }
    
    public void UnloadNpcObjects()
    {
        if(npcList.Count == 0)
        {
            return;
        }
        npcLogicScripts.Clear();
        foreach (var npc in npcList)
        {
            npc.SetActive(false);
        }
    }
    public Overworld_interactable CheckForNpcPosition(Vector3 positionToCheck)
    {
        foreach (var npc in npcLogicScripts)
        {
            if (npc.movementHandler.Moving)
            {
                continue;
            }
            if (npc.transform.position != positionToCheck)
            {
                continue;
            }
            return npc.npcInteractable;
        }
        return null;
    }
}
