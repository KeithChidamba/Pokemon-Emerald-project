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
    
    private GameLoadingHandler _gameLoadingHandler;
    private PlayerMovementHandler _playerMovementHandler;
    
    public void Inject(ServiceContainer container)
    {
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
        currentArea?.UnloadNpcObjects();
        currentArea = newArea;
        currentArea.LoadNpcObjects(npcPrefab,overworldNpcParent);
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
