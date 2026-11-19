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
    public Transform overworldPickupParent;
    public event Action<Item> OnItemPickedUp;

    [SerializeField] private StoryObjectiveRegistry storyObjectiveRegistry;
    [HideInInspector] public StoryProgress storyProgress;
    private Dictionary<StoryObjectiveSection,List<StoryObjective>> _storyObjectiveGroups = new();
    private Dictionary<string,StoryObjective> _cachedObjectives = new();
    [SerializeField] private StoryObjectiveSection currentObjectiveGroup;
    /// <summary>
    /// For debugging view
    /// </summary>
    [SerializeField] private List<StoryObjective> currentStoryObjectives;
    
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
        _cachedObjectives.Clear();
        foreach (var group in storyObjectiveRegistry.storyObjectiveGroups)
        {
            foreach (var objective in group.storyObjectives)
            { 
                _cachedObjectives.Add(objective.name,objective);
            }
        }
    }

    public void StartDataLoad()
    {
        StartCoroutine(LoadOverworldState());
    }
    public void NotifyTreesOfObjective(Item berryForObjective,bool active)
    {
        foreach (var tree in overworldBerryTrees)
        {
            tree.handlingObjective = active;
            tree.currentBerryForObjective = berryForObjective;
        }
    }
    
    private void LoadDefaultTrees()
    {
        for(var i = 0;i<treeRegistry.soilGroups.Count;i++ )
        {
            var overworldTreeData = treeRegistry.soilGroups[i];
            if (!overworldTreeData.loadedFromJson)
            {
                var leftOverTree = overworldTreeData;
                var newSoilObject = Instantiate(berrySoilPrefab, leftOverTree.treePosition, berryTreesParent.rotation, berryTreesParent);
                newSoilObject.SetActive(true);

                var soilTransform = newSoilObject.transform;
                var treeIndex = 0;

                for (var c = 0; c < soilTransform.childCount; c++)
                {
                    var berryTree = soilTransform.GetChild(c).GetComponent<BerryTree>();
                    if (berryTree == null) continue;   // not a tree, don't consume a data slot

                    if (treeIndex >= leftOverTree.treeData.Count)
                    {
                        Debug.LogWarning($"Soil has more BerryTrees than treeData entries ({leftOverTree.treeData.Count}).");
                        break;
                    }

                    overworldBerryTrees.Add(berryTree);
                    berryTree.Inject(_container);
                    berryTree.LoadDefaultAsset(leftOverTree.treeData[treeIndex], i);
                    treeIndex++;
                }

                if (treeIndex < leftOverTree.treeData.Count)
                    Debug.LogWarning($"Only {treeIndex} BerryTrees found for {leftOverTree.treeData.Count} treeData entries.");
            }
        }
    }

    private void LoadSavedTrees()
    {
        var soilTrees = new Dictionary<int, BerryTree[]>();
        var slotLoaded = new Dictionary<int, bool[]>();

        foreach (var treeData in jsonLoadedTreeData)
        {
            var overworldSoilData = treeRegistry.soilGroups[treeData.soilIndex];

            if (!soilTrees.TryGetValue(treeData.soilIndex, out var berryTrees))
            {
                overworldSoilData.numTreesLoaded = 0;
                overworldSoilData.loadedFromJson = true;

                var newSoilObject = Instantiate(berrySoilPrefab, overworldSoilData.treePosition,
                    berryTreesParent.rotation, berryTreesParent);
                newSoilObject.SetActive(true);

                var soilTransform = newSoilObject.transform;
                var treeList = new List<BerryTree>();
                for (var j = 0; j < soilTransform.childCount; j++)
                {
                    var t = soilTransform.GetChild(j).GetComponent<BerryTree>();
                    if (t != null) treeList.Add(t);
                }

                berryTrees = treeList.ToArray();

                overworldBerryTrees.AddRange(berryTrees);
                soilTrees[treeData.soilIndex] = berryTrees;
                slotLoaded[treeData.soilIndex] = new bool[berryTrees.Length];
            }

            var slot = treeData.slotIndex;
            if (slot < 0 || slot >= berryTrees.Length || slotLoaded[treeData.soilIndex][slot]) continue;

            var tree = berryTrees[slot];

            if (tree == null)
            {
                Debug.LogError($"Null tree: soil {treeData.soilIndex}, slot {slot}, soil children {berryTrees.Length}");
                continue;
            }

            tree.Inject(_container);
            tree.LoadTreeData(treeData);

            slotLoaded[treeData.soilIndex][slot] = true;
            overworldSoilData.numTreesLoaded++;
            
        }
    }
   
    private IEnumerator LoadOverworldState()
    {
        storyProgress = null;
        
        //load default story objectives
        _storyObjectiveGroups.Clear();
        foreach (var group in storyObjectiveRegistry.storyObjectiveGroups)
        {
            _storyObjectiveGroups[group.section] = new List<StoryObjective>();
            _storyObjectiveGroups[group.section].AddRange(group.storyObjectives);
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
            foreach (var pickupData in overworldPickupRegistry.overworldPickups)
            {
                _loadedPickupRegistry.overworldPickups.Add(new PickupData(pickupData.pickup, false));
            }
        }
        _loadedPickupRegistry.LoadLookup(overworldPickupPrefab, overworldPickupParent);
        // empty; just stores prefab/parent
        _objectivePickupRegistry.LoadLookup(overworldPickupPrefab, overworldPickupParent); 

        //story objectives
        if (storyProgress is null)
        {
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
                currentObjectiveGroup = storyProgress.lastActiveGroup;
                var maxNum = _storyObjectiveGroups[currentObjectiveGroup].Count;
                _storyObjectiveGroups[currentObjectiveGroup].RemoveRange(0,maxNum-storyProgress.numRemainingInGroup);
            }
        }
        yield return new WaitForSeconds(0.025f);
        
        //load current objective
        if(!storyProgress.allObjectivesComplete)
        {
            FindMainStoryAsset(_container, _storyObjectiveGroups[currentObjectiveGroup][0].name);
            currentStoryObjectives = _storyObjectiveGroups[currentObjectiveGroup];
        }
        yield return new WaitForSeconds(0.025f);
    }
    private void FindMainStoryAsset(ServiceContainer container,string mainAssetName)
    {
        if (_cachedObjectives.TryGetValue(mainAssetName, out var mainAsset))
        {
            mainAsset.LoadObjective(container);
        }
        else
        {
            Debug.LogError("Story objective Asset: "+mainAssetName+" not found");
        }
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
            FindMainStoryAsset(_container, _storyObjectiveGroups[currentObjectiveGroup][0].name);
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
    private static int GetTreeSlot(BerryTree tree)
    {
        var parent = tree.transform.parent;
        var slot = 0;
        for (var i = 0; i < tree.transform.GetSiblingIndex(); i++)
        {
            if (parent.GetChild(i).GetComponent<BerryTree>() != null) slot++;
        }
        return slot;
    }
    public void SaveOverworldData()
    {
        var newBerrySave = new BerryTreeSaveRegistry();
        foreach (var tree in overworldBerryTrees)
        {
            var treeSave = new BerryTreeSaveData();
            treeSave.SaveData(tree.treeData);
            treeSave.slotIndex = GetTreeSlot(tree);
            newBerrySave.savedTrees.Add(treeSave);
        }
        _saveHandler.SaveDataAsJson(newBerrySave, "Tree Registry", SaveDataDirectory.BerryTrees);
        
        _saveHandler.SaveDataAsJson(_loadedPickupRegistry,"Item pickup registry",SaveDataDirectory.OverworldItemPickupRegistry);
       
        int objectiveCount = 0;
        foreach (var pair in _storyObjectiveGroups)
        {
            var group = pair.Value;
            objectiveCount += group.Count;
        }
        
        storyProgress.allObjectivesComplete = objectiveCount == 0;
        
        if (storyProgress.allObjectivesComplete)
        {
            storyProgress.lastActiveGroup = currentObjectiveGroup;
            storyProgress.numRemainingInGroup = 0;
        }
        else
        {
            var currentGroupCount = _storyObjectiveGroups[currentObjectiveGroup].Count;
            storyProgress.numRemainingInGroup = currentGroupCount;
        }
        storyProgress.lastActiveGroup = currentObjectiveGroup;
        _saveHandler.SaveDataAsJson(storyProgress,"Story Progress",SaveDataDirectory.StoryObjectiveProgress);
    }
}
