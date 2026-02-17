using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;


public class Area_manager : MonoBehaviour
{
    public AreaData currentArea;
    public AreaData[] overworldAreas;
    public bool loadingPlayerFromSave;
    public static Area_manager Instance;
    public Tilemap groundTileMap;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

        if (areaName == currentArea.data.areaName) return;

        currentArea.LoadNpcObjects(false);
        var area = overworldAreas.First(a=>a.data.areaName == areaName);
        area.LoadNpcObjects(true);
        currentArea = area;
        Game_Load.Instance.playerData.location = currentArea.data.areaName;
    }
       
    
}
