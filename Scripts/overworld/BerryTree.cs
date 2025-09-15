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
        primaryInteractable = GetComponent<Overworld_interactable>();
        Options_manager.Instance.OnInteractionTriggered += HarvestBerries;
        Options_manager.Instance.OnInteractionTriggered += WaterTree;
        Options_manager.Instance.OnInteractionTriggered += ChooseBerryToPlant;
        if (isPlanted) return;
        
        primaryInteractable.interaction = plantInteraction;
        primaryInteractable.interactionType = Overworld_interactable.InteractionType.PlantBerry;
    }

    private void Update()
    {
        if (!isPlanted) return;
        if (treeData.currentStageProgress > 3) return;

        treeData.minutesSinceLastStage += Time.deltaTime;
        if (treeData.minutesSinceLastStage >= treeData.minutesPerStage)
        {
            //new stage
            treeData.minutesSinceLastStage = 0;
            treeData.currentStageProgress++;
            
            if (treeData.currentStageProgress == 4)
            {
                treeData.currentStageNeedsWater = false;
                primaryInteractable.interaction = harvestInteraction;
                primaryInteractable.interactionType = Overworld_interactable.InteractionType.PickBerry;
                return;
            }

            treeData.currentStageNeedsWater = true;
            primaryInteractable.interaction = waterInteraction;
            primaryInteractable.interactionType = Overworld_interactable.InteractionType.WaterBerryTree;
        }
    }
    
    private void WaterTree(Overworld_interactable interactable)
    {
        if (interactable.interactionType != Overworld_interactable.InteractionType.WaterBerryTree) return;
        
        if (!treeData.currentStageNeedsWater)
        {
            //display, tree already watered
            return;
        }
        
        numStagesWatered++;
        treeData.currentStageNeedsWater = false;
        primaryInteractable.interaction = idleInteraction;
        primaryInteractable.interactionType = Overworld_interactable.InteractionType.None;
    }

    private void ChooseBerryToPlant(Overworld_interactable interactable)
    {
        if (isPlanted)
        {
            //display regular message
            return;
        }
        if (interactable.interactionType != Overworld_interactable.InteractionType.PlantBerry) return;
        Bag.Instance.ViewBag();
        //allow berry selection, exiting cancels event
    }
    public void PlantBerry(Item itemToPlant)//call this from bag
    {
        //get the berry being planted
        //set tree data by resource load, from item name
        isPlanted = true;
        numStagesWatered = 0;
        primaryInteractable.interactionType = Overworld_interactable.InteractionType.PlantBerry;
    }
    private int GetBerryYield()
    {
        var bracket1= (treeData.maxYield - treeData.minYield) / 4;
        var bracket2= bracket1 * numStagesWatered;
        return treeData.minYield + bracket2 + Utility.RandomRange(0,bracket1);
    }
    private void HarvestBerries(Overworld_interactable interactable)
    {
        if (interactable.interactionType != Overworld_interactable.InteractionType.PickBerry) return;
        
        isPlanted = false;
        primaryInteractable.interaction = idleInteraction;
        
        var berries = Obj_Instance.CreateItem(treeData.berryItem);
        berries.quantity = GetBerryYield();
        Bag.Instance.AddItem(berries);
        Dialogue_handler.Instance.DisplayDetails($"You picked up {berries.quantity}" +
                                                 $" {berries.itemName}'s",2f);
    }
}

