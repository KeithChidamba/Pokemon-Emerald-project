using System.Linq;
using UnityEngine;

public class Area_manager : MonoBehaviour,IInjectable
{
    public AreaData currentArea;
    public AreaData[] overworldAreas;
    public bool loadingPlayerFromSave;
    
    private Game_Load _gameLoadingHandler;

    public void Inject(Container container)
    {
        _gameLoadingHandler = container.Resolve<Game_Load>();
    }

    public void EscapeArea()
    {
        //inside this method in-case there extra stuff that needs to happen when escaping
    }

    public void SwitchToArea(AreaName areaName)
    {
        if(loadingPlayerFromSave)
        {
            var saveArea = overworldAreas.First(a=>a.data.areaName == areaName);
            currentArea = saveArea;
            loadingPlayerFromSave = false;
        }
        else
        {
            if (areaName == currentArea.data.areaName) return;
        }
        
        currentArea.LoadNpcObjects(false);
        var area = overworldAreas.First(a=>a.data.areaName == areaName);
        area.LoadNpcObjects(true);
        currentArea = area;
        _gameLoadingHandler.playerData.location = currentArea.data.areaName;
    }
       
    
}
