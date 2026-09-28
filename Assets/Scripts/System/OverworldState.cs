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

    [SerializeField] private OverworldPickupRegistry overworldPickupRegistry;
    private OverworldPickupRegistry _loadedPickupRegistry;
    private OverworldPickupRegistry _objectivePickupRegistry;
    [SerializeField] private GameObject overworldPickupPrefab;
    [SerializeField] private Transform overworldPickupParent;
    public event Action<Item> OnItemPickedUp;

    [SerializeField] private StoryObjectiveRegistry storyObjectiveRegistry;
    public List<StoryObjective> currentStoryObjectives = new();
    [SerializeField]private StoryProgressObjective storyProgressObjective;
    
    public event Action OnObjectivesLoaded;
    private SaveDataHandler _saveHandler;
    private DialogueHandler _dialogueHandler;
    private ServiceContainer _container;
    private GameLoadingHandler _gameLoadingHandler;
    private PlayerBagHandler _playerBag;
    
    public void Inject(ServiceContainer container)
    {
        _container = container;
        _saveHandler = container.Resolve<SaveDataHandler>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _gameLoadingHandler = container.Resolve<GameLoadingHandler>();
        _playerBag = container.Resolve<PlayerBagHandler>();
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
        
        _loadedPickupRegistry = null;
        foreach (Transform child in overworldPickupParent)
        {
            Destroy(child.gameObject);
        }
        
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
                    }
                    else
                    {
                        var treesPerSoil = 4;//always 4
                        var currentTree = overworldBerryTrees[((soilCreated - 1) * treesPerSoil) + overworldSoilData.numTreesLoaded];
                        currentTree.Inject(_container);
                        currentTree.LoadTreeData(treeData); 
                    }
                    overworldSoilData.numTreesLoaded++;
                }
            }
            else LoadDefaultTrees();
        }
        else LoadDefaultTrees();
        
        //overworld pickups
        _objectivePickupRegistry = ScriptableObject.CreateInstance<OverworldPickupRegistry>();
        if (_loadedPickupRegistry is null)
        {
            _loadedPickupRegistry = ScriptableObject.CreateInstance<OverworldPickupRegistry>();
            foreach (var authored in overworldPickupRegistry.overworldPickups)
            {
                _loadedPickupRegistry.overworldPickups.Add(new PickupData(authored.pickup, false));
            }
        }
        _loadedPickupRegistry.LoadLookup(overworldPickupPrefab, overworldPickupParent);
        // empty; just stores prefab/parent
        _objectivePickupRegistry.LoadLookup(overworldPickupPrefab, overworldPickupParent); 

        //story objectives
        if (storyProgressObjective is null)
        {
            currentStoryObjectives.AddRange(storyObjectiveRegistry.allStoryObjectives); 
            yield return new WaitUntil(() => currentStoryObjectives.Count==storyObjectiveRegistry.allStoryObjectives.Count);
            currentStoryObjectives.ForEach(o=>o.mainAssetName=o.name);
            
            storyProgressObjective = Resources.Load<StoryProgressObjective>(DirectoryHandler.GetDirectory(AssetDirectory.StoryObjectiveData)+"Story Progress");
            storyProgressObjective.mainAssetName = storyProgressObjective.name;
            storyProgressObjective.totalObjectiveAmount = storyObjectiveRegistry.allStoryObjectives.Count;
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
        yield return new WaitForSeconds(0.025f);
    }

    public bool PickupItemFound(Vector2 interactionPosition)
    {
        var handlingPickupObjective = false;
        if (currentStoryObjectives.Count > 0)
        {
            handlingPickupObjective = currentStoryObjectives[0].objectiveType == StoryObjectiveType.PickupItem;
        }
        
        var itemPicked = handlingPickupObjective
            ? _objectivePickupRegistry.TakeItemPickup(interactionPosition)
              ?? _loadedPickupRegistry.TakeItemPickup(interactionPosition)
            : _loadedPickupRegistry.TakeItemPickup(interactionPosition);        
        
        if (itemPicked is not null)
        {
            _playerBag.AddItem(itemPicked);
            var quantityMessage = itemPicked.quantity > 1 ? "'s" : "";
            _dialogueHandler.DisplayDetails($"Picked up {itemPicked.quantity} {itemPicked.itemName}{quantityMessage}");
            OnItemPickedUp?.Invoke(itemPicked);
            return true;
        }
        return false;
    }
    public void LoadItemPickups(OverworldPickupRegistry saved)
    {
        var pickedIds = new HashSet<string>(
            saved.overworldPickups
                .Where(p => p.hasBeenPicked).Select(p => p.pickupId));

        _loadedPickupRegistry = ScriptableObject.CreateInstance<OverworldPickupRegistry>();
        foreach (var authored in overworldPickupRegistry.overworldPickups)
        {
            _loadedPickupRegistry.overworldPickups.Add(
                new PickupData(authored.pickup, pickedIds.Contains(authored.pickup.name)));
        }
    }
    public void AddPickup(OverworldPickup pickup)
    {
        var newPickupData = new PickupData(pickup, false);
        _objectivePickupRegistry.AddToLookUp(newPickupData);
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
        
        _saveHandler.SaveItemPickupDataAsJson(_loadedPickupRegistry,"Item pickup registry");
        yield return new WaitForSeconds(0.02f);

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
