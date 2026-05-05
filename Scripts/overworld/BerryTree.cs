using System;
using UnityEngine;

public class BerryTree : MonoBehaviour,IInjectable
{
    public Overworld_interactable primaryInteractable;
    public Interaction harvestInteraction;
    public Interaction plantInteraction;
    public Interaction waterInteraction;
    public Interaction idleInteraction;
    public BerryTreeData treeData;

    public SpriteRenderer treeSpriteRenderer;
    private int _currentSpriteIndex;

    public int treeIndex;
    [SerializeField] float secondsCounter;

    public event Action<bool> OnInteractionComplete;

    private Dialogue_handler _dialogueHandler;
    private OverworldState _overworldStateHandler;
    private DialogueOptionsEventHandler _gameLoadHandler;
    private Bag _playerBag;
    private Game_ui_manager _gameUIHandler;
    private InputStateHandler _inputStateHandler;
    private overworld_actions _overworldActions;
    private DialogueOptionsEventHandler _dialogueOptionsHandler;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueOptionsHandler = container.Resolve<DialogueOptionsEventHandler>();
        _overworldActions = container.Resolve<overworld_actions>();
        _playerBag = container.Resolve<Bag>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _overworldStateHandler = container.Resolve<OverworldState>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        OnInject();
    }

    private void OnInject()
    {
        _dialogueOptionsHandler.OnOverworldInteractionOptionChosen += ChooseBerryToPlant;
        _dialogueOptionsHandler.OnOverworldInteractionOptionChosen += HarvestBerries;
        _dialogueOptionsHandler.OnOverworldInteractionOptionChosen += WaterTree;
        gameObject.SetActive(true);
        primaryInteractable = GetComponent<Overworld_interactable>();
    }
    
    private void SetInteraction(OverworldInteractionType type)
    {
        primaryInteractable.interaction = type switch
        {
            OverworldInteractionType.PlantBerry => plantInteraction,
            OverworldInteractionType.PickBerry => harvestInteraction,
            OverworldInteractionType.WaterBerryTree => waterInteraction,
            _ => idleInteraction
        };
        primaryInteractable.interaction.overworldInteraction = type;
    }

    public void LoadDefaultAsset()
    {
        var copy = InstanceFactory.CreateTreeData(treeData);
        treeData = null;
        treeData = copy;
        treeData.isPlanted = true;
        treeData.treeObjectName = name;
        treeData.currentStageProgress = 4;
        treeData.currentStageNeedsWater = false;
        SetInteraction(OverworldInteractionType.PickBerry);
    }

    public void LoadTreeData(BerryTreeData tree)
    {
        treeData = tree;
        treeSpriteRenderer.sprite = treeData.isPlanted? treeData.GetTreeSprite()[0] : null;
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
        {
            treeData.minutesSinceLastStage = 0;
            treeData.currentStageNeedsWater = false;
        }

        if (!treeData.isPlanted)
        {
            SetInteraction(OverworldInteractionType.PlantBerry);
            return;
        }
        
        if (treeData.currentStageNeedsWater)
        {
            SetInteraction(OverworldInteractionType.WaterBerryTree);
        }
        else
        { 
            var interactionType = treeData.currentStageProgress == 4
                ? OverworldInteractionType.PickBerry
                : OverworldInteractionType.None;
            SetInteraction(interactionType);
        }
    }
    private void Update()
    {
        if (treeData is not { isPlanted: true }) return;
        
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
            
            if (treeData.currentStageProgress == 4)
            {
                treeData.currentStageNeedsWater = false;
                SetInteraction(OverworldInteractionType.PickBerry);
                return;
            }

            treeData.currentStageNeedsWater = true;
            SetInteraction(OverworldInteractionType.WaterBerryTree);
        }
    }

    private void WaterTree(Overworld_interactable interactable,int optionChosen)
    {
        if (interactable != primaryInteractable) return;
        if (interactable.interaction.overworldInteraction != OverworldInteractionType.WaterBerryTree) return;
        
        if (optionChosen > 0)
        {
            _dialogueHandler.EndDialogue(); 
                OnInteractionComplete?.Invoke(false);
            return;
        }
        _dialogueHandler.DeletePreviousOptions();
        
        if (!_playerBag.SearchForItem("Wailmer Pail"))
        {
            OnInteractionComplete?.Invoke(false);
            _dialogueHandler.DisplayDetails("You need the correct item for this");
            return;
        }
        if (!treeData.currentStageNeedsWater)
        {
            OnInteractionComplete?.Invoke(false);
            _dialogueHandler.DisplayDetails("You have already watered this plant");
            return;
        }
        StartCoroutine(_overworldActions.WaterTrees());
        treeData.numStagesWatered++;
        treeData.currentStageNeedsWater = false;
        OnInteractionComplete?.Invoke(true);
        SetInteraction(OverworldInteractionType.None);
    }

    private void ChooseBerryToPlant(Overworld_interactable interactable,int optionChosen)
    {
        if (treeData.isPlanted) return;
        if (interactable != primaryInteractable) return;
        if (interactable.interaction.overworldInteraction != OverworldInteractionType.PlantBerry) return;
        
        if (optionChosen > 0)
        {
            OnInteractionComplete?.Invoke(false);
            _dialogueHandler.EndDialogue(); 
            return;
        }
        _dialogueHandler.DeletePreviousOptions();
        _playerBag.OnItemSelected += PlantBerry;
        _playerBag.currentBagUsage = BagUsage.SelectionOnly;
        _gameUIHandler.ViewBag();
    }
    private void PlantBerry(Item berryToPlant)
    {
        if (berryToPlant.itemType != ItemType.Berry)
        {
            OnInteractionComplete?.Invoke(false);
            _dialogueHandler.DisplayDetails("Only berries can be planted");
            return;
        }
        _playerBag.OnItemSelected -= PlantBerry;
        
        treeData.numStagesWatered = 0;
        
        
        SetInteraction(OverworldInteractionType.WaterBerryTree);
        var treeDataAsset = Resources.Load<BerryTreeData>(
            SaveDataHandler.GetDirectory(AssetDirectory.BerryTreeData)
                                                          + berryToPlant.itemName+" Data");
        treeData = InstanceFactory.CreateTreeData(treeDataAsset);
        
        treeData.isPlanted = true;
        _inputStateHandler.ResetGroupUi(InputStateGroup.Bag);
        
        _dialogueHandler.DisplayDetails($"You planted a {berryToPlant.itemName}");
        OnInteractionComplete?.Invoke(true);
    }
    private int GetBerryYield()
    {
        var bracket1= (treeData.maxYield - treeData.minYield) / 4;
        var bracket2= bracket1 * treeData.numStagesWatered;
        return treeData.minYield + bracket2 + Utility.RandomRange(0,bracket1);
    }
    private void HarvestBerries(Overworld_interactable interactable, int optionChosen)
    {
        if (interactable != primaryInteractable) return;
        if (interactable.interaction.overworldInteraction != OverworldInteractionType.PickBerry) return;

        if (optionChosen > 0)
        {
            OnInteractionComplete?.Invoke(false);
            _dialogueHandler.EndDialogue(); 
            return;
        }
        _dialogueHandler.DeletePreviousOptions();
        
        var berries = InstanceFactory.CreateItem(treeData.berryItem);
        berries.quantity = GetBerryYield();
        _playerBag.AddItem(berries);
        _dialogueHandler.DisplayDetails($"You picked up {berries.quantity}" +
                                                 $" {berries.itemName}'s");
        
        treeSpriteRenderer.sprite = null;
        treeData.isPlanted = false;
        treeData.currentStageProgress = 0;
        OnInteractionComplete?.Invoke(true);
        SetInteraction(OverworldInteractionType.PlantBerry);
    }
    public void ChangeSprite()//animation Event
    {
        if (treeData is not { isPlanted: true }) return;
        
        _currentSpriteIndex = _currentSpriteIndex==1? 0 : _currentSpriteIndex+1;
        treeSpriteRenderer.sprite = treeData.GetTreeSprite()[_currentSpriteIndex];
    }
}

