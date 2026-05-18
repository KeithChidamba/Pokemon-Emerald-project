using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OverworldState : MonoBehaviour,IInjectable
{    
    [SerializeField]private List<BerryTree> overworldBerryTrees = new();
    [SerializeField]private List<BerryTreeData> treeDataQueue = new();
    
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
        OnInject();
    }

    private void OnInject()
    {
        _gameLoadingHandler.OnGameStarted += StartDataLoad;
    }

    private void StartDataLoad()
    {
        StartCoroutine(LoadOverworldState());
    }

    private IEnumerator LoadOverworldState()
    {
        overworldBerryTrees.Clear();
        currentStoryObjectives.Clear();
        treeDataQueue.Clear();
        overworldPickups.Clear();
        _overworldPickupPositions.Clear();
        
        var trees = FindObjectsOfType<BerryTree>(true);
        foreach(var tree in trees)
        {
            tree.loadedFromJSON = false;
            overworldBerryTrees.Add(tree);
        }

        var pickups = Resources.LoadAll<OverworldPickup>(SaveDataHandler.GetDirectory(AssetDirectory.OverworldItemPickups));
        overworldPickups.AddRange(pickups);
        
        yield return new WaitForSeconds(0.25f);
        
        if (_gameLoadingHandler.LoadedFromSave)
        {
            yield return _saveHandler.LoadOverworldData();
            
            foreach (var treeData in treeDataQueue)
            {
                var jsonBerryTree = overworldBerryTrees.First(tree=>tree.gameObject.name == treeData.treeObjectName);
                jsonBerryTree.name = treeData.itemAssetName + " Tree";//visual debugging purposes
                jsonBerryTree.LoadTreeData(treeData);
            }

            foreach (var tree in overworldBerryTrees)
            {
                if (!tree.loadedFromJSON)
                {
                    tree.LoadDefaultAsset();
                }
            }

            foreach (var pickup in overworldPickups)
            {
                if(pickup.hasBeenPicked)continue;
                _overworldPickupPositions.Add(pickup.itemPosition,pickup);
                var newPickupObject = Instantiate(overworldPickupPrefab,pickup.itemPosition,overworldPickupPrefab.transform.rotation, overworldPickupParent);
                newPickupObject.SetActive(true);
            }
            
        }
        else
        {
            overworldBerryTrees.ForEach(tree => tree.LoadDefaultAsset());
        }
        
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
            _dialogueHandler.DisplayDetails($"Picked up {itemPicked.quantity} {itemPicked.itemName}'s");
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
        treeDataQueue.Add(treeData);
    }
    public IEnumerator SaveOverworldData()
    {
        foreach (var tree in overworldBerryTrees)
        {
            tree.treeData.SetLastLogin(DateTime.Now);
            tree.treeData.itemAssetName = tree.treeData.berryItem.itemName;
            _saveHandler.SaveBerryTreeDataAsJson(tree.treeData,tree.treeData.treeObjectName);
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
