using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Area_manager : MonoBehaviour
{
    [FormerlySerializedAs("current_area")] public Switch_Area currentArea;
    [FormerlySerializedAs("Areas")] public Switch_Area[] overworldAreas;
    [FormerlySerializedAs("loadingPLayerFromSave")] public bool loadingPlayerFromSave;
    private Switch_Area _areaBuilding;
    public Sprite[] areaBoards;
    public int currentAreaIndex;
    public Image routeDisplayBoard;
    public Image cityDisplayBoard;
    public Text cityDisplayName;
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
    public void EnterBuilding(Switch_Area area,float loadTime=0f)
    {
        if (area.areaData.hasDoorAnimation)
            area.doorAnimation.Play("Open");
        currentArea = area;
        Invoke(nameof(LoadBuilding),loadTime);
    }
    public void SwitchToArea(AreaData.AreaName areaName,float loadTime=0f)
    {
        if (areaName!=AreaData.AreaName.OverWorld)
        {
            var area = FindArea(areaName);
            EnterBuilding(area, loadTime);
        }
        else
            GoToOverworld();
    }
    private Switch_Area FindArea(AreaData.AreaName areaName)
    {
        return overworldAreas.FirstOrDefault(a=>a.areaData.areaName==areaName);
    }
    public void GoToOverworld()
    {
        if (_areaBuilding != null)
        {//from building to over world
            _areaBuilding.interior.SetActive(false);
            _areaBuilding.areaData.insideArea = false;
            Player_movement.Instance.playerObject.transform.position = _areaBuilding.doorPosition.position;
            Player_movement.Instance.RestrictPlayerMovement();
        }
        else //from save point in overworld
            Player_movement.Instance.playerObject.transform.position = Game_Load.Instance.playerData.playerPosition;
        foreach (var area in overworldAreas)
            area.overworld.SetActive(true);
        if (_areaBuilding != null)
            if (_areaBuilding.areaData.hasDoorAnimation)
                _areaBuilding.doorAnimation.Play("Close");
        Invoke(nameof(ResetPlayerMovement), 1f);
        currentArea = FindArea(AreaData.AreaName.OverWorld);
        Player_movement.Instance.canUseBike = true;
        Game_Load.Instance.playerData.location = currentArea.areaData.areaName;
        _areaBuilding = null;
    }
    private void LoadBuilding()
    {
        foreach (var area in overworldAreas)
            area.overworld.SetActive(false);
        overworld_actions.Instance.doingAction = false;
        Player_movement.Instance.RestrictPlayerMovement();
        currentArea.areaData.insideArea = true;
        currentArea.interior.SetActive(true);
        _areaBuilding = currentArea;
        if(!loadingPlayerFromSave)
            Player_movement.Instance.playerObject.transform.position = currentArea.doormatPosition.position;
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
