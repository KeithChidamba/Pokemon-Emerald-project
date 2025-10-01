using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class BerryTree : MonoBehaviour
{
    public Overworld_interactable primaryInteractable;
    public Interaction harvestInteraction;
    public Interaction plantInteraction;
    public Interaction waterInteraction;
    public Interaction idleInteraction;
    public BerryTreeData treeData;

    public bool isPlanted;
    public SpriteRenderer treeSpriteRenderer;
    private int _currentSpriteIndex;
    public event Action OnTreeAwake;
    [SerializeField]float secondsCounter;
    private void Awake()
    {
        primaryInteractable = GetComponent<Overworld_interactable>();
        Options_manager.Instance.OnInteractionTriggered += HarvestBerries;
        Options_manager.Instance.OnInteractionTriggered += WaterTree;
        Options_manager.Instance.OnInteractionTriggered += ChooseBerryToPlant;
        Save_manager.Instance.OnOverworldDataLoaded += LoadDefaultAsset;
        treeSpriteRenderer = GetComponent<SpriteRenderer>();
        OnTreeAwake?.Invoke();
        if (isPlanted) return;
        
        SetInteraction(Overworld_interactable.InteractionType.PlantBerry);
    }

    private void SetInteraction(Overworld_interactable.InteractionType type)
    {
        primaryInteractable.interaction = type switch
        {
            Overworld_interactable.InteractionType.PlantBerry => plantInteraction,
            Overworld_interactable.InteractionType.PickBerry => harvestInteraction,
            Overworld_interactable.InteractionType.WaterBerryTree => waterInteraction,
            _ => idleInteraction
        };
        primaryInteractable.interactionType = type;
    }
    private void LoadDefaultAsset()
    {
        //loads default Asset if there's no save data, only happen when a new tree is made during dev
        if (treeData is { loadedFromJson: false })
        {
            var copy = Obj_Instance.CreateTreeData(treeData);
            treeData = null;
            treeData = copy;
            treeData.treeIndex = OverworldState.Instance.GetTreeIndex(this);
        }
    }
    public void LoadTreeData(BerryTreeData tree)
    {
        treeData = tree;
        treeSpriteRenderer.sprite = treeData.GetTreeSprite()[0];

        var lastLoginTime = treeData.GetLastLogin();
        
        TimeSpan timeDifference = DateTime.Now - lastLoginTime;

        int minutesPassed = (int)timeDifference.TotalMinutes;
        int stagesPassed = minutesPassed / treeData.minutesPerStage;
        int leftOverMinutes = minutesPassed % treeData.minutesPerStage;
        

// Update growth
        treeData.currentStageProgress += stagesPassed;
        treeData.currentStageProgress = Math.Clamp(treeData.currentStageProgress, 0, 4);

// Only track leftover minutes if not max stage
        if (treeData.currentStageProgress < 4)
            treeData.minutesSinceLastStage += leftOverMinutes;
        else
            treeData.minutesSinceLastStage = 0;
        
        if (treeData.currentStageNeedsWater)
        {
            primaryInteractable.interaction = waterInteraction;
            SetInteraction(Overworld_interactable.InteractionType.WaterBerryTree);
        }
        else
        { 
            var interactionType = treeData.currentStageProgress == 4
                ? Overworld_interactable.InteractionType.PickBerry
                : Overworld_interactable.InteractionType.None;
            SetInteraction(interactionType);
        }
        
        OnTreeAwake = null;
    }
    private void Update()
    {
        if (!isPlanted) return;
        if (treeData.currentStageProgress > 3) return;
        
        secondsCounter += Time.deltaTime; 
        if (secondsCounter >= 60f)
        {
            int minutesPassed = (int)(secondsCounter / 60f);
            treeData.minutesSinceLastStage += minutesPassed;
            secondsCounter -= minutesPassed * 60f;
        }
        
        if (treeData.minutesSinceLastStage >= treeData.minutesPerStage)
        {
            //new stage
            treeData.minutesSinceLastStage = 0;
            treeData.currentStageProgress++;
            
            treeSpriteRenderer.sprite = treeData.GetTreeSprite()[0];
            
            if (treeData.currentStageProgress == 4)
            {
                treeData.currentStageNeedsWater = false;
                SetInteraction(Overworld_interactable.InteractionType.PickBerry);
                return;
            }

            treeData.currentStageNeedsWater = true;
            SetInteraction(Overworld_interactable.InteractionType.WaterBerryTree);
        }
    }
    
    private void WaterTree(Overworld_interactable interactable)
    {
        if (interactable.interactionType != Overworld_interactable.InteractionType.WaterBerryTree) return;

        if (!Bag.Instance.SearchForItem("Wailmer Pail"))
        {
            Dialogue_handler.Instance.DisplayDetails("You need the correct item for this",2f);
            return;
        }
        if (!treeData.currentStageNeedsWater)
        {
            Dialogue_handler.Instance.DisplayDetails("You have already watered this plant",2f);
            return;
        }
        
        treeData.numStagesWatered++;
        treeData.currentStageNeedsWater = false;
        SetInteraction(Overworld_interactable.InteractionType.None);
    }

    private void ChooseBerryToPlant(Overworld_interactable interactable)
    {
        if (isPlanted) return;
        
        if (interactable.interactionType != Overworld_interactable.InteractionType.PlantBerry) return;
        
        Bag.Instance.ViewBag();
        //allow berry selection, exiting cancels event
    }
    public void PlantBerry(Item itemToPlant)//call this from bag
    {
        //get the berry being planted
        //set tree data by resource load, from item name
        isPlanted = true;
        treeData.numStagesWatered = 0;
        SetInteraction(Overworld_interactable.InteractionType.WaterBerryTree);
    }
    private int GetBerryYield()
    {
        var bracket1= (treeData.maxYield - treeData.minYield) / 4;
        var bracket2= bracket1 * treeData.numStagesWatered;
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
        treeSpriteRenderer.sprite = null;
    }
    public void ChangeSprite()
    {
        _currentSpriteIndex = _currentSpriteIndex==1? 0 : _currentSpriteIndex+1;
        treeSpriteRenderer.sprite = treeData.GetTreeSprite()[_currentSpriteIndex];
    }
}

