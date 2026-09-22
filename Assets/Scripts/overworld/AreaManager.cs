using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AreaName
{
    PlayerHome,LittleRootTown,OldaleTown,PokeMartOldale,PokeCenterOldale,Route101,Route102,Route104
}
public class AreaManager : MonoBehaviour,IInjectable
{
    public AreaData currentArea;
    public AreaData[] overworldAreas;
    
    private GameLoadingHandler _gameLoadingHandler;
    private PlayerMovementHandler _playerMovementHandler;
    
    public void Inject(ServiceContainer container)
    {
        _gameLoadingHandler = container.Resolve<GameLoadingHandler>();
        _playerMovementHandler = container.Resolve<PlayerMovementHandler>();
    }

    public void OnInject()
    {
        
    }
    public void EscapeArea()
    {
        if (currentArea.locationData.escapable)
        {
            SwitchToArea(currentArea.locationData.overworldAreaName);
            _playerMovementHandler.SetPlayerPosition(currentArea.locationData.entranceCell);
        }
    }
    public void TeleportToArea(AreaName areaName)
    {
        var exiting = currentArea.locationData.areaName == areaName;
        var area = overworldAreas.First(a=>a.locationData.areaName == areaName);
        if (exiting)
        {
            SwitchToArea(area.locationData.overworldAreaName);
            _playerMovementHandler.SetPlayerPosition(currentArea.locationData.entranceCell);
        }
        else
        {
            SetArea(area);
            _playerMovementHandler.SetPlayerPosition(currentArea.locationData.exitCell);
        }
    }
    /// <summary>
    /// prevent repeated area loading when touching adjacent tiles
    /// </summary>
    public void SwitchToAreaNoTeleport(AreaName areaName)
    {
        if (currentArea.locationData.areaName == areaName)
        {
            return;
        }
        SwitchToArea(areaName);
    }
    public void SwitchToArea(AreaName areaName)
    {
        var area = overworldAreas.First(a=>a.locationData.areaName == areaName);
        SetArea(area);
    }
    private void SetArea(AreaData newArea)
    {
        currentArea.UnloadNpcObjects();
        currentArea = newArea;
        currentArea.LoadNpcObjects();
        _gameLoadingHandler.playerData.location = currentArea.locationData.areaName;
    }
    
    public static string GetAreaName(AreaName areaValue)
    {
        var areaNames = new Dictionary<AreaName, string>
        {
            {AreaName.LittleRootTown,"Garden"},
            {AreaName.PokeMartOldale,"PokeMart Oldale"},
            {AreaName.PokeCenterOldale,"Poke-Center"},
        };
        return areaNames[areaValue];
    }
}
