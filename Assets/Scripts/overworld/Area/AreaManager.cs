using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AreaName
{
    OpenGarden,BattleZone,PokeMart,PokeCenter,Route101,Route102
}
public class AreaManager : MonoBehaviour,IInjectable
{
    public AreaData currentArea;
    public List<AreaData> overworldAreas = new();
    public Transform storyNpcParent;
    public Transform overworldNpcParent;
    public GameObject npcPrefab;
    
    [SerializeField]private List<NpcLogic> npcLogicScripts = new();
    [SerializeField]private List<GameObject> npcObjects = new();
    
    private GameLoadingHandler _gameLoadingHandler;
    private PlayerMovementHandler _playerMovementHandler;
    private ServiceContainer _container;
    public void Inject(ServiceContainer container)
    {
        _container = container;
        _gameLoadingHandler = container.Resolve<GameLoadingHandler>();
        _playerMovementHandler = container.Resolve<PlayerMovementHandler>();
    }

    public void OnInject()
    {
        gameObject.SetActive(true);
        overworldAreas.Clear();
        var areas =  Resources.LoadAll<AreaData>(DirectoryHandler.GetDirectory(AssetDirectory.Areas));
        overworldAreas.AddRange(areas);
    }
    
    public OverworldInteractable CheckForNpcPosition(Vector3 positionToCheck)
    {
        foreach (var npc in npcLogicScripts)
        {
            if (npc.movementHandler.Moving)
            {
                continue;
            }
            if (npc.transform.position != positionToCheck)
            {
                continue;
            }
            return npc.npcInteractable;
        }
        return null;
    }
    
    public void TeleportToArea(AreaName areaName)
    {
        SwitchToArea(areaName);
        _playerMovementHandler.SetPlayerPosition(currentArea.entranceCell);
    }
    public void SwitchToArea(AreaName areaName)
    {
        var area = overworldAreas.First(a=>a.areaName == areaName);
        SetArea(area);
    }
    private void SetArea(AreaData newArea)
    {
        if (currentArea is not null)
        {
            if (currentArea.npcList.Count > 0)
            {
                foreach (var obj in npcObjects)
                {
                    Destroy(obj);
                }
                npcObjects.Clear();
                npcLogicScripts.Clear();
            }
        }
        
        currentArea = newArea;
        if (currentArea.npcList.Count > 0)
        {
            foreach (var npc in currentArea.npcList)
            {
                var npcInstance = Instantiate(npcPrefab, npc.npcData.npcPosition, npcPrefab.transform.rotation,
                    overworldNpcParent);
                var npcLogic = npcInstance.GetComponentInChildren<NpcLogic>();
                
                npcLogic.Inject(_container,npc);
                
                npcLogicScripts.Add(npcLogic);
                npcObjects.Add(npcInstance);
                npcInstance.SetActive(true);
            }
        }

        _gameLoadingHandler.playerData.location = currentArea.areaName;
        DetermineSongForArea(currentArea.areaName);
    }

    private static void DetermineSongForArea(AreaName areaName)
    {
        var choice = areaName switch
        {
            AreaName.OpenGarden => MusicId.Littleroot,
            AreaName.PokeCenter => MusicId.PokemonCenter,
            AreaName.PokeMart => MusicId.PokeMart,
            AreaName.Route101 => MusicId.Route101,
            AreaName.Route102 => MusicId.Route104,
            _ => MusicId.PetalburgCity
        };
        SoundManager.PlayMusic(choice);
    }
    public static string GetAreaName(AreaName areaValue)
    {
        var areaNames = new Dictionary<AreaName, string>
        {
            {AreaName.OpenGarden,"Open Garden"},
            {AreaName.PokeMart,"PokeMart"},
            {AreaName.PokeCenter,"Poke-Center"},
            {AreaName.Route101,"Route101"},
            {AreaName.Route102,"Route102"},
            {AreaName.BattleZone,"Battle Zone"},
        };
        return areaNames[areaValue];
    }
}
