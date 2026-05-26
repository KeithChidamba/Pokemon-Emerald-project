using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OverworldState : MonoBehaviour,IInjectable
{    
    [SerializeField]private List<BerryTreeData> jsonLoadedTreeData = new();
    [SerializeField] private List<BerryTree> overworldBerryTrees = new();
    [SerializeField] private BerryTreeRegistry treeRegistry;
    [SerializeField] private GameObject berrySoilPrefab;
    [SerializeField] private Transform berryTreesParent;

    [SerializeField] private List<OverworldPickup> overworldPickups = new ();
    private Dictionary<Vector2,OverworldPickup> _overworldPickupPositions = new();
    [SerializeField] private GameObject overworldPickupPrefab;
    [SerializeField] private Transform overworldPickupParent;
    
    public List<StoryObjective> allStoryObjectives = new();
    public List<StoryObjective> currentStoryObjectives = new();
    public StoryProgressObjective storyProgressObjective;
    
    
    public event Action OnObjectivesLoaded;
    private SaveDataHandler _saveHandler;
    private Dialogue_handler _dialogueHandler;
    private ServiceContainer _container;
    private Game_Load _gameLoadingHandler;
    private Bag _playerBag;
    
    public void Inject(ServiceContainer container)
    {
        _container = container;
        _saveHandler = container.Resolve<SaveDataHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        _playerBag = container.Resolve<Bag>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _gameLoadingHandler.OnGameStarted += StartDataLoad;
    }

    private void StartDataLoad()
    {
        StartCoroutine(LoadOverworldState());
    }

    private void LoadDefaultTrees()
    {
        for(var i = 0;i<treeRegistry.soilGroups.Count;i++ )
        {
            var overworldTreeData = treeRegistry.soilGroups[i];
            if (!overworldTreeData.loadedFromJson)
            {
                var leftOverTree = overworldTreeData;
                var newSoilObject = Instantiate(berrySoilPrefab,leftOverTree.treePosition,berryTreesParent.rotation, berryTreesParent);
                newSoilObject.SetActive(true);
                var berryTrees = newSoilObject
                    .GetComponentsInChildren<BerryTree>(true)
                    .OrderBy(t => t.transform.GetSiblingIndex())
                    .ToArray();
                overworldBerryTrees.AddRange(berryTrees);
                for(var j = 0;j<berryTrees.Length;j++ )
                {
                    berryTrees[j].Inject(_container);
                    berryTrees[j].LoadDefaultAsset(overworldTreeData.treeData[j],i);
                }
            }
        }
    }
    private IEnumerator LoadOverworldState()
    {
        currentStoryObjectives.Clear();
        jsonLoadedTreeData.Clear();
        overworldBerryTrees.Clear();
        _overworldPickupPositions.Clear();
        
        var pickups = Resources.LoadAll<OverworldPickup>(SaveDataHandler.GetDirectory(AssetDirectory.OverworldItemPickups));
        overworldPickups.AddRange(pickups);
        yield return new WaitForSeconds(0.25f);
        
        if (_gameLoadingHandler.LoadedFromSave)
        {
            yield return _saveHandler.LoadOverworldData();
        }
        
        //berry trees
        foreach(var tree in treeRegistry.soilGroups)
        {
            tree.loadedFromJson = false;
        }
        if (_gameLoadingHandler.LoadedFromSave)
        {
            jsonLoadedTreeData = jsonLoadedTreeData
                .OrderBy(x => x.soilIndex)
                .ToList();
            if(jsonLoadedTreeData.Count>0)
            {
                var soilCreated = 0;
                foreach (var treeData in jsonLoadedTreeData)
                {
                    var overworldSoilData = treeRegistry.soilGroups[treeData.soilIndex];
                    
                    if(!overworldSoilData.loadedFromJson)
                    {
                        overworldSoilData.numTreesLoaded = 0;
                        overworldSoilData.loadedFromJson = true;
                        soilCreated++;
                        var newSoilObject = Instantiate(berrySoilPrefab, overworldSoilData.treePosition,
                            berryTreesParent.rotation, berryTreesParent);
                        newSoilObject.SetActive(true);
                        var berryTrees = newSoilObject
                            .GetComponentsInChildren<BerryTree>(true)
                            .OrderBy(t => t.transform.GetSiblingIndex())
                            .ToArray();
                        overworldBerryTrees.AddRange(berryTrees);
                        berryTrees[0].Inject(_container);
                        berryTrees[0].LoadTreeData(treeData); 
                        overworldSoilData.numTreesLoaded++;
                    }
                    else
                    {
                        var treesPerSoil = 4;//always 4
                        var currentTree = overworldBerryTrees[((soilCreated - 1) * treesPerSoil) + overworldSoilData.numTreesLoaded];
                        currentTree.Inject(_container);
                        currentTree.LoadTreeData(treeData); 
                        overworldSoilData.numTreesLoaded++;
                    }
                }
            }
            else LoadDefaultTrees();
        }
        else LoadDefaultTrees();
        
        //overworld pickups
        foreach (var pickup in overworldPickups)
        {
            if(pickup.hasBeenPicked)continue;
            _overworldPickupPositions.Add(pickup.itemPosition,pickup);
            var newPickupObject = Instantiate(overworldPickupPrefab,pickup.itemPosition,overworldPickupPrefab.transform.rotation, overworldPickupParent);
            newPickupObject.SetActive(true);
        }
        
        //story objectives
        if (storyProgressObjective == null)
        {
            currentStoryObjectives.AddRange(allStoryObjectives); 
            yield return new WaitUntil(() => currentStoryObjectives.Count==allStoryObjectives.Count);
            currentStoryObjectives.ForEach(o=>o.mainAssetName=o.name);
            
            storyProgressObjective = Resources.Load<StoryProgressObjective>(SaveDataHandler.GetDirectory(AssetDirectory.StoryObjectiveData)+"Story Progress");
            storyProgressObjective.mainAssetName = storyProgressObjective.name;
            storyProgressObjective.totalObjectiveAmount = allStoryObjectives.Count;
            storyProgressObjective.numCompleted = 0;
            
        }
        else
        {
            var orderList = currentStoryObjectives.OrderBy(obj => obj.indexInList).ToList();
            currentStoryObjectives.Clear();
            currentStoryObjectives.AddRange(orderList);
            yield return new WaitUntil(() => currentStoryObjectives.Count==orderList.Count);
        }
        OnObjectivesLoaded?.Invoke();
        if (storyProgressObjective.numCompleted < storyProgressObjective.totalObjectiveAmount)
        {
            currentStoryObjectives[0].FindMainAsset(_container);
        }
    }

    public bool PickupItemFound(Vector2 interactionPosition)
    {
        if (_overworldPickupPositions.TryGetValue(interactionPosition, out var pickupItem))
        {
            pickupItem.hasBeenPicked = true;
            var itemPicked = InstanceFactory.CreateItem(pickupItem.item);
            _playerBag.AddItem(itemPicked);
            var quantityMessage = itemPicked.quantity > 1 ? "'s" : "";
            _dialogueHandler.DisplayDetails($"Picked up {itemPicked.quantity} {itemPicked.itemName}{quantityMessage}");
            return true;
        }
        return false;
    }

    public void LoadItemPickups(OverworldPickup pickUpSaveData)
    {
        var desiredItem = overworldPickups.First(pickup => pickup.item.itemName == pickUpSaveData.itemAssetName);
        desiredItem.hasBeenPicked = pickUpSaveData.hasBeenPicked;
    }
    public bool HasObjective(string objectiveName)
    {
        return currentStoryObjectives.Any(obj=>obj.mainAssetName == objectiveName);
    }

    public void LoadStoryProgress(StoryProgressObjective storyData)
    {
        storyProgressObjective = storyData;
        storyProgressObjective.FindMainAsset(_container);
    }
    public void ClearAndLoadNextObjective()
    {
        currentStoryObjectives.RemoveAt(0);
        storyProgressObjective.numCompleted++;
        if (currentStoryObjectives.Count > 0)
        {
            currentStoryObjectives[0].FindMainAsset(_container);
        }
        else
        {
            _dialogueHandler.RemoveObjectiveText();
        }
    }

    public void StoreBerryTreeData(BerryTreeData treeData)
    {
        jsonLoadedTreeData.Add(treeData);
    }
    public IEnumerator SaveOverworldData()
    {
        foreach (var tree in overworldBerryTrees)
        {
            tree.treeData.SetLastLogin(DateTime.Now);
            var randomID = Utility.Random16Bit();//prevent duplicate json file names
            _saveHandler.SaveBerryTreeDataAsJson(tree.treeData,$"{tree.treeData.berryItem.itemName} {randomID}");
        }
        yield return new WaitForSeconds(1f);
        
        foreach (var pickup in overworldPickups)
        {
            pickup.itemAssetName = pickup.item.itemName;
            _saveHandler.SaveItemPickupDataAsJson(pickup,pickup.itemAssetName);
            yield return new WaitForSeconds(0.02f);
        }
        
        int objectiveIndex=0;
        foreach (var objective in currentStoryObjectives)
        {
            objective.mainAssetName = objective.mainAssetName==string.Empty? objective.name:objective.mainAssetName;
            objective.indexInList = objectiveIndex;
            objectiveIndex++;
            _saveHandler.SaveStoryDataAsJson(objective,objective.objectiveHeading);
            yield return new WaitForSeconds(0.025f);
        }
        storyProgressObjective.mainAssetName = storyProgressObjective.mainAssetName==string.Empty? storyProgressObjective.name:storyProgressObjective.mainAssetName;
        _saveHandler.SaveStoryDataAsJson(storyProgressObjective,"Story Progress");
        yield return null;
    }
}
