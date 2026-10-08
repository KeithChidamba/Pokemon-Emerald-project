using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "berry obj", menuName = "Objectives/berry interaction objective")]
public class BerryInteractionObjective : StoryObjective
{
    private DialogueHandler _dialogueHandler;
    private OverworldState _overworldState;
    
    public Item berryForObjective;
    public OverworldInteractionType interactionTypeForObjective;
    
    private BerryTree _watchedTree;

    protected override void OnObjectiveLoaded()
    {
        _dialogueHandler = serviceContainer.Resolve<DialogueHandler>();
        _overworldState = serviceContainer.Resolve<OverworldState>();
        _watchedTree = null;
        _dialogueHandler.DisplayObjectiveText(objectiveHeading);
        _dialogueHandler.OnOptionsDisplayed += CheckInteractionTriggered;
    }

    private void CheckInteractionTriggered(OverworldInteractable interactable)
    {
        if (interactionTypeForObjective != interactable.interaction.overworldInteraction) return;

        var berryTree = interactable.GetComponent<BerryTree>();
        if (berryTree is null) return;

        StopWatchingTree();
        _watchedTree = berryTree;
        _watchedTree.OnInteractionComplete += CheckEventSuccess;
    }

    private void CheckEventSuccess(bool successful)
    {
        var tree = _watchedTree;
        StopWatchingTree();

        if (!successful || tree is null) return;
        if (berryForObjective.itemName != tree.treeData.berryItem.itemName) return;

        ClearObjective();
    }

    private void StopWatchingTree()
    {
        if (_watchedTree is null) return;
        _watchedTree.OnInteractionComplete -= CheckEventSuccess;
        _watchedTree = null;
    }

    protected override void OnObjectiveCleared()
    {
        _dialogueHandler.OnOptionsDisplayed -= CheckInteractionTriggered;
        StopWatchingTree();
        _overworldState.ClearAndLoadNextObjective();
    }
}
