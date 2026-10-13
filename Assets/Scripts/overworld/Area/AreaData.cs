using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Overworld/Area Transition Data")]
public class AreaData : ScriptableObject
{
    public AreaName areaName;
    public Vector3 entranceCell;
    public Biome biome;
    public List<NpcCreationData> npcList = new();
    [SerializeField]private List<NpcLogic> npcLogicScripts = new();
    
    public void LoadNpcObjects(GameObject npcPrefab,Transform storyNpcParent)
    {
        if(npcList.Count == 0)
        {
            return;
        }
        foreach (var npc in npcList)
        {
            var npcInstance = Instantiate(npcPrefab, npc.npcPosition,npcPrefab.transform.rotation,storyNpcParent);
            var npcLogic = npcInstance.GetComponentInChildren<NpcLogic>();
            npcLogicScripts.Add(npcLogic);
            npcLogic.movementHandler.animationData = npc.npcAnimationData;
            if (npc.npcInteraction is null)
            {
                var newInteraction = CreateInstance<Interaction>();
                newInteraction.dialogueType = DialogType.Details;
                newInteraction.interactionMessage =  npc.npcInteractionMessage;
                npcLogic.npcInteractable.interaction = newInteraction;
            }
            else
            {
                npcLogic.npcInteractable.interaction = npc.npcInteraction;
            }
            npcInstance.SetActive(true);
        }
    }
    public void UnloadNpcObjects()
    {
        if(npcList.Count == 0)
        {
            return;
        }
        foreach (var npc in npcLogicScripts)
        {
            Destroy(npc.gameObject);
        }
        npcLogicScripts.Clear();
    }
    public OverworldInteractable CheckForNpcPosition(Vector3 positionToCheck)
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
