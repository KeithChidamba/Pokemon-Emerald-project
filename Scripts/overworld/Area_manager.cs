using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Area_manager : MonoBehaviour
{
    [FormerlySerializedAs("current_area")] public Switch_Area currentArea;
    [FormerlySerializedAs("Areas")] public Switch_Area[] overworldAreas;
    public bool loadingPLayerFromSave;
    private Switch_Area _areaBuilding;
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
    public void SwitchToArea(Switch_Area area,float loadTime)
    {
        if (area.hasDoorAnimation)
            area.doorAnimation.Play("Open");
        currentArea = area;
        Invoke(nameof(LoadBuilding),loadTime);
    }
    public void SwitchToArea(string areaName,float loadTime)
    {
        if (areaName!="Overworld")
        {
            var area = FindArea(areaName);
            if (area.hasDoorAnimation)
                area.doorAnimation.Play("Open");
            currentArea = area;
            Invoke(nameof(LoadBuilding), loadTime);
        }
        else
            GoToOverworld();
    }
    private Switch_Area FindArea(string areaName)
    {
        foreach (Switch_Area a in overworldAreas)
            if (a.areaName == areaName)
                return a;
        return null;
    }
    public void GoToOverworld()
    {
        if (_areaBuilding != null)
        {//from building to over world
            _areaBuilding.interior.SetActive(false);
            _areaBuilding.insideArea = false;
            Player_movement.instance.transform.position = _areaBuilding.doorPosition.localPosition;
            Player_movement.instance.canmove = false;
        }
        else //from save point in overworld
            Player_movement.instance.transform.position = Game_Load.Instance.playerData.playerPosition;
        foreach (var area in overworldAreas)
            area.overworld.SetActive(true);
        if (_areaBuilding != null)
            if (_areaBuilding.hasDoorAnimation)
                _areaBuilding.doorAnimation.Play("Close");
        Invoke(nameof(ResetPlayerMovement), 1f);
        currentArea = FindArea("Overworld");
        Player_movement.instance.can_use_bike = true;
        Game_Load.Instance.playerData.location = currentArea.areaName;
        _areaBuilding = null;
    }
    private void LoadBuilding()
    {
        foreach (var area in overworldAreas)
            area.overworld.SetActive(false);
        overworld_actions.Instance.doingAction = false;
        Player_movement.instance.canmove = false;
        currentArea.insideArea = true;
        currentArea.interior.SetActive(true);
        _areaBuilding = currentArea;
        if(!loadingPLayerFromSave)
            Player_movement.instance.transform.position = currentArea.doormatPosition.position;
        Invoke(nameof(ResetPlayerMovement), 1f);
        Game_Load.Instance.playerData.location = currentArea.areaName;
    }
    private void ResetPlayerMovement()
    {
        Player_movement.instance.canmove = true;
        loadingPLayerFromSave = false;
    }
}
