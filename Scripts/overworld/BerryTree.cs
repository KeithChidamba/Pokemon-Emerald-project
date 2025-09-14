using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BerryTree : MonoBehaviour
{
    public Overworld_interactable primaryInteractable;
    public Interaction harvestInteraction;
    public Interaction plantInteraction;
    public Interaction waterInteraction;
    public Interaction idleInteraction;
    public BerryTreeData treeData;
    public int numStagesWatered;
    public bool isPlanted;

    private void Awake()
    {
        Options_manager.Instance.OnInteractionTriggered += HarvestBerries;
        Options_manager.Instance.OnInteractionTriggered += WaterTree;
        Options_manager.Instance.OnInteractionTriggered += ChooseBerryToPlant;
    }

    private void Update()
    {
        if (!isPlanted) return;
        if (treeData.currentStageProgress > 3) return;

        treeData.minutesSinceLastStage += Time.deltaTime;
        if (treeData.minutesSinceLastStage >= treeData.minutesPerStage)
        {
            treeData.minutesSinceLastStage = 0;
            treeData.currentStageProgress++;
            treeData.currentStageNeedsWater = true;

            if (treeData.currentStageProgress == 4)
            {
                treeData.currentStageNeedsWater = false;
                primaryInteractable.interaction = harvestInteraction;
            }

            primaryInteractable.interaction = treeData.currentStageNeedsWater ? waterInteraction : idleInteraction;
        }
    }
    
    private void WaterTree(Overworld_interactable interactable)
    {
        if (interactable.interactionType != "Berry Water") return;
        numStagesWatered++;
    }

    private void ChooseBerryToPlant(Overworld_interactable interactable)
    {
        if (isPlanted)
        {
            //display regular message
            return;
        }
        if (interactable.interactionType != "Berry Plant") return;
        Bag.Instance.ViewBag();
        //allow berry selection, exiting cancels event
    }
    public void PlantBerry(Item itemToPlant)
    {
        //get the berry being planted
        //set tree data by resource load, from item name
        isPlanted = true;
        numStagesWatered = 0;
    }
    private int GetBerryYield()
    {
        var bracket1= (treeData.maxYield - treeData.minYield) / 4;
        var bracket2= bracket1 * numStagesWatered;
        return treeData.minYield + bracket2 + Utility.RandomRange(0,bracket1);
    }
    private void HarvestBerries(Overworld_interactable interactable)
    {
        if (interactable.interactionType != "Berry Pick") return;
        
        isPlanted = false;
        primaryInteractable.interaction = idleInteraction;
        
        var berries = Obj_Instance.CreateItem(treeData.berryItem);
        berries.quantity = GetBerryYield();
        Bag.Instance.AddItem(berries);
        Dialogue_handler.Instance.DisplayDetails($"You picked up {berries.quantity}" +
                                                 $" {berries.itemName}'s",2f);
    }
}

