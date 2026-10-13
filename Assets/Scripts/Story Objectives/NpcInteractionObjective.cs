
using UnityEngine;

[CreateAssetMenu(fileName = "npc obj", menuName = "Objectives/Npc Interaction objective")]
public class NpcStoryObjective : StoryObjective
{
    public string npcName;
    public NpcCreationData npcData;
    public bool removeAfterInteraction;
    public bool facePlayer;
    
    private GameObject npcInstance;
    protected NpcLogic npcLogic;
    
    private DialogueHandler _dialogueHandler;
    private AreaManager _areaManager;
    private PlayerMovementHandler _playerMovement;
    
    protected override void OnObjectiveLoaded()
    {
        _dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        _areaManager = serviceContainer.Resolve<AreaManager>(); 
        _playerMovement = serviceContainer.Resolve<PlayerMovementHandler>();
        _dialogueHandler.DisplayObjectiveText(objectiveHeading==string.Empty?$"Speak to {npcName}":objectiveHeading);
        SetupNpc(_areaManager);
        _dialogueHandler.OnDialogueEnded += ClearAfterInteractionDialogue;
    }
    
    protected void SetupNpc(AreaManager areaManager)
    {
        var hasChild = areaManager.storyNpcParent.childCount > 0;
        if (hasChild)
        {
            Destroy(areaManager.storyNpcParent.GetChild(0).gameObject);
        }
        
        npcInstance = Instantiate(areaManager.npcPrefab, npcData.npcPosition,areaManager.npcPrefab.transform.rotation,areaManager.storyNpcParent);
        npcLogic = npcInstance.GetComponentInChildren<NpcLogic>();
        npcLogic.movementHandler.isControlled = true;
        npcLogic.movementHandler.animationData.isIdle = true;
        
        npcLogic.movementHandler.animationData = npcData.npcAnimationData;
        if (npcData.npcInteraction is null)
        {
            var newInteraction = CreateInstance<Interaction>();
            newInteraction.dialogueType = DialogType.Details;
            newInteraction.interactionMessage =  npcData.npcInteractionMessage;
            npcLogic.npcInteractable.interaction = newInteraction;
        }
        else
        {
            npcLogic.npcInteractable.interaction = npcData.npcInteraction;
        }
        npcInstance.SetActive(true);

        if (facePlayer)
        {
            _dialogueHandler.OnDialogueStarted += FacePlayer;
            npcLogic.OnObjectDestroy += () =>
            {
                _dialogueHandler.OnDialogueStarted -= FacePlayer;
            };
        }
    }
    
    private void FacePlayer(Interaction interaction)
    {
        if (interaction.GetID != npcLogic.npcInteractable.interaction.GetID) return; 
        npcLogic.movementHandler.FacePlayerDirection(_playerMovement);
    }

    private void ClearAfterInteractionDialogue(Interaction interaction)
    {
        if (interaction is null) return;
        if (interaction.GetID != npcLogic.npcInteractable.interaction.GetID) return; 
        _dialogueHandler.OnDialogueEnded -= ClearAfterInteractionDialogue;
        ClearAndHandleRemoval();
    }
    protected void ClearAndHandleRemoval()
    {
        if (removeAfterInteraction)
        {
            Destroy(npcInstance);
        }
        ClearObjective();
    }
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}