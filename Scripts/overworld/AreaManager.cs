using System.Linq;
using UnityEngine;

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
        //inside this method in-case there extra stuff that needs to happen when escaping
    }
    public void LoadAreaFromSave(AreaName areaName)
    {
        var saveArea = overworldAreas.First(a=>a.data.areaName == areaName);
        SetArea(saveArea);
    }
    public void TeleportToArea(AreaName areaName)
    {
        SwitchToArea(areaName);
        _playerMovementHandler.SetPlayerPosition(currentArea.tileLocation);
    }
    public void SwitchToArea(AreaName areaName)
    {
        if (areaName == currentArea.data.areaName) return;
        var area = overworldAreas.First(a=>a.data.areaName == areaName);
        SetArea(area);
    }

    private void SetArea(AreaData newArea)
    {
        currentArea.UnloadNpcObjects();
        newArea.LoadNpcObjects();
        currentArea = newArea;
        _gameLoadingHandler.playerData.location = currentArea.data.areaName;
    }
}
