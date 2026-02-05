using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;


public class Area_manager : MonoBehaviour
{
    public AreaTransitionData currentArea;
    public AreaTransitionData[] overworldAreas;
    public bool loadingPlayerFromSave;
    public static Area_manager Instance;
    public Tilemap doorTileMap;
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
        GoToOverworld();
    }
    public void EnterBuilding(AreaTransitionData area,float loadTime=0f)
    {
        currentArea = area;
        Invoke(nameof(LoadBuilding),loadTime);
    }
    public void SwitchToArea(AreaName areaName,float loadTime=0f)
    {
        if (areaName!=AreaName.OverWorld)
        {
            var area = FindArea(areaName);
            EnterBuilding(area, loadTime);
        }
        else
            GoToOverworld();
    }
    private AreaTransitionData FindArea(AreaName areaName)
    {
        return overworldAreas.FirstOrDefault(a=>a.areaData.areaName==areaName);
    }
    public void GoToOverworld()
    {
        if (currentArea != null)
        {//from building to over world
            Vector3 doorPos = currentArea.GetTeleportWorldPosition(doorTileMap);
            Player_movement.Instance.SetPlayerPosition(doorPos);
            Player_movement.Instance.RestrictPlayerMovement();
            currentArea.areaData.insideArea = false;
        }
        else //from save point in overworld
            Player_movement.Instance.SetPlayerPosition(Game_Load.Instance.playerData.playerPosition);
           
        Invoke(nameof(ResetPlayerMovement), 1f);
        Player_movement.Instance.canUseBike = true;
        Game_Load.Instance.playerData.location = AreaName.OverWorld;
    }
    
    private void LoadBuilding()
    {
        overworld_actions.Instance.doingAction = false;
        Player_movement.Instance.RestrictPlayerMovement();
        Player_movement.Instance.canUseBike = false;
        
        if(!loadingPlayerFromSave)
        {
            Vector3 spawnPos = currentArea.GetTeleportWorldPosition(doorTileMap);
            Player_movement.Instance.SetPlayerPosition(spawnPos);
            currentArea.areaData.insideArea = true;
        }
        
        Player_movement.Instance.ForceWalkMovement();
        Invoke(nameof(ResetPlayerMovement), 1f);
        Game_Load.Instance.playerData.location = currentArea.areaData.areaName;
    }
    private void ResetPlayerMovement()
    {
        Player_movement.Instance.AllowPlayerMovement();
        loadingPlayerFromSave = false;
    }
}
