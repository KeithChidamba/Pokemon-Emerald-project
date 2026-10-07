using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class OverworldState : MonoBehaviour,IInjectable
{    
    [SerializeField] private List<BerryTreeData> jsonLoadedTreeData = new();
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
    [HideInInspector] public StoryProgress storyProgress;
    private Dictionary<StoryObjectiveSection,List<StoryObjectiveSave>> _storyObjectiveGroups = new();
    [SerializeField] private StoryObjectiveSection currentObjectiveGroup;
    /// <summary>
    /// For debugging view
    /// </summary>
    [SerializeField] private List<StoryObjectiveSave> currentStoryObjectives;
    
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

    private void LoadSavedTrees()
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
    private IEnumerator LoadOverworldState()
    {
        storyProgress = null;
        _storyObjectiveGroups.Clear();
        foreach (var group in storyObjectiveRegistry.storyObjectiveGroups)
        {
            _storyObjectiveGroups.Add(group.section, new List<StoryObjectiveSave>());
        }
        
        jsonLoadedTreeData.Clear();
        overworldBerryTrees.Clear();
        
        _loadedPickupRegistry = null;
        foreach (Transform child in overworldPickupParent)
        {
            Destroy(child.gameObject);
        }
        
        yield return new WaitForSeconds(0.025f);
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
            if(jsonLoadedTreeData.Count > 0)
            {
                LoadSavedTrees();
            }
            else LoadDefaultTrees();
        }else LoadDefaultTrees();
        
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
        if (storyProgress is null)
        {
            //load default story objectives
            foreach (var group in storyObjectiveRegistry.storyObjectiveGroups)
            {
                _storyObjectiveGroups[group.section] = new List<StoryObjectiveSave>();
                foreach (var objective in group.storyObjectives)
                {
                    var objectiveSaveData = new StoryObjectiveSave
                    {
                        mainAssetName = objective.name,
                        objectiveType = objective.objectiveType,
                        groupSection = group.section
                    };
                    _storyObjectiveGroups[group.section].Add(objectiveSaveData);
                }
            }
            storyProgress = new StoryProgress { allObjectivesComplete = false };
            currentObjectiveGroup = _storyObjectiveGroups.First().Key;
        }
        else
        {
            if (storyProgress.allObjectivesComplete)
            {
                _storyObjectiveGroups.Clear();
            }
            else
            {
                //remove completed objectives
                var sections = (StoryObjectiveSection[])Enum.GetValues(typeof(StoryObjectiveSection));
                foreach (var section in sections)
                {
                    if (section < storyProgress.lastActiveGroup)
                    {
                        _storyObjectiveGroups.Remove(section);
                    }
                }
                foreach (var pair in _storyObjectiveGroups)
                {
                    //sort objectives in order
                    var currentGroup = pair.Value;
                    var orderList = currentGroup.OrderBy(obj => obj.indexInList).ToList();
                    currentGroup.Clear();
                    currentGroup.AddRange(orderList);
                }
                currentObjectiveGroup = storyProgress.lastActiveGroup;
            }
        }
        yield return new WaitForSeconds(0.025f);
        
        //load current objective
        OnObjectivesLoaded?.Invoke();
        if(!storyProgress.allObjectivesComplete)
        {
            FindMainStoryAsset(_container, _storyObjectiveGroups[currentObjectiveGroup][0].mainAssetName);
            currentStoryObjectives = _storyObjectiveGroups[currentObjectiveGroup];
        }
        yield return new WaitForSeconds(0.025f);
    }
    private void FindMainStoryAsset(ServiceContainer container,string mainAssetName)
    {
        //because story objective aren't loaded in a performance heavy context
        //we can get away with loading the main asset this way each time
        string dir = DirectoryHandler.GetDirectory(AssetDirectory.StoryObjectiveData);
        StoryObjective[] all = Resources.LoadAll<StoryObjective>(dir);
        var mainAsset = Array.Find(all, o => o.name == mainAssetName);
        if (mainAsset is null)
        {
            Debug.LogError("Story objective Asset: "+mainAssetName+" not found");
            return;
        }
        mainAsset.LoadObjective(container);
    }
    public void LoadStoryObjective(StoryObjectiveSave objectiveSave)
    {
        _storyObjectiveGroups[objectiveSave.groupSection].Add(objectiveSave);
    }
    public void CheckForItemAtPosition(Vector2 interactionPosition)
    {
        var handlingPickupObjective = false;
        if (_storyObjectiveGroups.Count > 0)
        {
            handlingPickupObjective = _storyObjectiveGroups[currentObjectiveGroup][0]
                .objectiveType == StoryObjectiveType.PickupItem;
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
            SoundManager.Play(JingleId.LevelUp);
        }
    }
    public void LoadItemPickups(OverworldPickupRegistry saved)
    {
        var pickedIds = new HashSet<string>(
            saved.overworldPickups
                .Where(p => p.hasBeenPicked)
                .Select(p => p.pickupId));

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
    
    public bool HasObjective(StoryObjective objectiveData)
    {
        foreach (var pair in _storyObjectiveGroups)
        {
            var group = pair.Value;
            foreach (var save in group)
            {
                if (save.mainAssetName == objectiveData.name)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    public void ClearAndLoadNextObjective()
    {
        _storyObjectiveGroups[currentObjectiveGroup].RemoveAt(0);
       
        if (_storyObjectiveGroups[currentObjectiveGroup].Count == 0)
        {
            _storyObjectiveGroups.Remove(currentObjectiveGroup);
        }
      
        if (_storyObjectiveGroups.Count > 0)
        {
            currentObjectiveGroup = _storyObjectiveGroups.First().Key;
            FindMainStoryAsset(_container, _storyObjectiveGroups[currentObjectiveGroup][0].mainAssetName);
            currentStoryObjectives = _storyObjectiveGroups[currentObjectiveGroup];
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
            _saveHandler.SaveDataAsJson(tree.treeData,$"{tree.treeData.berryItem.itemName} {randomID}",SaveDataDirectory.BerryTrees);
        }
        yield return new WaitForSeconds(1f);
        
        _saveHandler.SaveDataAsJson(_loadedPickupRegistry,"Item pickup registry",SaveDataDirectory.OverworldItemPickupRegistry);
        yield return new WaitForSeconds(0.02f);

        int objectiveIndex = 0;
        foreach (var pair in _storyObjectiveGroups)
        {
            var group = pair.Value;
            foreach (var save in group)
            {
                save.indexInList = objectiveIndex;
                save.groupSection = pair.Key;
                _saveHandler.SaveDataAsJson(save,save.mainAssetName,SaveDataDirectory.StoryObjectives);
                yield return new WaitForSeconds(0.025f);
                objectiveIndex++;
            }
        }

        storyProgress.allObjectivesComplete = objectiveIndex == 0;
        storyProgress.lastActiveGroup = currentObjectiveGroup;
        _saveHandler.SaveDataAsJson(storyProgress,"Story Progress",SaveDataDirectory.StoryObjectiveProgress);
        
        yield return null;
    }
}
