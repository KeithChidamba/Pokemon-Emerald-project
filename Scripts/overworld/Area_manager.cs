using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Area_manager : MonoBehaviour
{
    public Switch_Area current_area;
    public Switch_Area[] Areas;
    public Player_movement player;
    public bool save_area = false;
    [SerializeField]Switch_Area area_building;
    public void Switch_Area(Switch_Area area,float load_time)
    {
        if (area.has_animation)
        {
            area.door.Play("Open");
        }
        current_area = area;
        Invoke(nameof(Load_area),load_time);
    }
    public void Switch_Area(string area_name,float load_time)
    {
        if (area_name != "Overworld")
        {
            Switch_Area a = find_area(area_name);   
            if (a.has_animation)
            {
                a.door.Play("Open");
            }
            current_area = a;
            Invoke(nameof(Load_area), load_time);
        }
        else
        {
            To_Over_world();
        }
    }
    private Switch_Area find_area(string area_name)
    {
        foreach (Switch_Area a in Areas)
        {
            if (a.area_name == area_name)
            {
                return a;
            }
        }
        return null;
    }
    public void To_Over_world()
    {
        if (area_building != null)
        {
            area_building.interior.SetActive(false);
            area_building.inside_area = false;
            player.transform.localPosition = area_building.door_pos.localPosition;
            player.canmove = false;
        }
        foreach (Switch_Area a in Areas)
        {
            a.overworld.SetActive(true);
        }
        if (area_building != null)
            if (area_building.has_animation)
            {
                area_building.door.Play("Close");
            }
        Invoke(nameof(reset_movement), 1f);
        current_area = find_area("Overworld");
        player.can_use_bike = true;
    }
    void Load_area()
    {
        foreach (Switch_Area a in Areas)
        {
            a.overworld.SetActive(false);
        }
        player.actions.doing_action = false;
        player.canmove = false;
        current_area.inside_area = true;
        current_area.interior.SetActive(true);
        area_building = current_area;
        if(!save_area)
            player.transform.position = current_area.Mat_pos.position;
        Invoke(nameof(reset_movement), 1f);
    }
    void reset_movement()
    {
        player.canmove = true;
        save_area = false;
    }
}
