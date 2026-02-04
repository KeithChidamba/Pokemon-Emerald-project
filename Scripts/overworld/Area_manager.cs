using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Area_manager : MonoBehaviour
{
    public AreaTransitionData currentArea;
    [FormerlySerializedAs("Areas")] public AreaTransitionData[] overworldAreas;
    public bool loadingPlayerFromSave;
    private AreaTransitionData _areaBuilding;
    public static Area_manager Instance;
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
        if (_areaBuilding != null)
        {//from building to over world
            _areaBuilding.interior.SetActive(false);
            Vector3 doorPos = _areaBuilding.GetDoorWorldPosition();
            Player_movement.Instance.SetPlayerPosition(doorPos);
            Player_movement.Instance.RestrictPlayerMovement();
        }
        else //from save point in overworld
            Player_movement.Instance.SetPlayerPosition(Game_Load.Instance.playerData.playerPosition);
        foreach (var area in overworldAreas)
            area.overworld.SetActive(true);
           
        Invoke(nameof(ResetPlayerMovement), 1f);
        currentArea = FindArea(AreaName.OverWorld);
        Player_movement.Instance.canUseBike = true;
        Game_Load.Instance.playerData.location = currentArea.areaData.areaName;
        _areaBuilding = null;
    }
    public void EnterArea(AreaTransitionData transition)
    {
        Vector3 spawnPos = transition.GetDoormatWorldPosition();
        Player_movement.Instance.SetPlayerPosition(spawnPos);
    }
    private void LoadBuilding()
    {
        foreach (var area in overworldAreas)
            area.overworld.SetActive(false);
        overworld_actions.Instance.doingAction = false;
        Player_movement.Instance.RestrictPlayerMovement();
        Player_movement.Instance.canUseBike = false;
        currentArea.interior.SetActive(true);
        _areaBuilding = currentArea;
        if(!loadingPlayerFromSave)
            Player_movement.Instance.SetPlayerPosition(_areaBuilding.GetDoormatWorldPosition());
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
