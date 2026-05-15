using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AreaData
{
    public AreaTransitionData data;
    public Vector3 tileLocation;
    public List<GameObject> npcList;
    [SerializeField]private List<Overworld_interactable> _npcInteractables=new();
    [SerializeField]private List<Vector3> npcPositions = new();
    private event Action OnNpcRemoved;
    
    public void LoadNpcObjects()
    {
        if(npcList.Count == 0)
        {
            return;
        }
        
        for (int i = 0; i < npcList.Capacity; i++)
        {
            var npc = npcList[i];
            var index = i;
            npc.SetActive(true);
            npcPositions.Add(npc.transform.position);
            var npcLogic = npc.GetComponentInChildren<NpcLogic>();
            OnNpcRemoved += npcLogic.movementHandler.ResetTileEvent;
            npcLogic.movementHandler.OnNewTile += (newPos) => SaveValidPositions(index,newPos);
            _npcInteractables.Add(npcLogic.npcInteractable);
        }
    }

    private void SaveValidPositions(int index,Vector3 newNpcPosition)
    {
        npcPositions[index] = newNpcPosition;
    }
    public void UnloadNpcObjects()
    {
        if(npcList.Count == 0)
        {
            return;
        }
        _npcInteractables.Clear();
        npcPositions.Clear();
        OnNpcRemoved?.Invoke();
        foreach (var npc in npcList)
        {
            npc.SetActive(false);
        }
        OnNpcRemoved = null;
    }
    public Overworld_interactable CheckForNpcPosition(Vector3 positionToCheck)
    {
        for (int i = 0; i < npcPositions.Capacity; i++)
        {
            if (npcPositions[i] != positionToCheck)
            {
                Debug.Log("far off");
                continue;
            }

            var distance = Vector3.Distance(npcPositions[i], npcList[i].transform.position);
            Debug.Log(distance);
            if (distance > 0.35f)
            {
                Debug.Log("skipped for long distance");
                continue;
            }
            return _npcInteractables[i];
        }
        return null;
    }
}
