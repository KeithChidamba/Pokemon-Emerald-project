using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Area_manager : MonoBehaviour
{
    public Switch_Area current_area;
    public Switch_Area[] Areas;
    public bool save_area = false;
    [SerializeField]Switch_Area area_building;
    public static Area_manager instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    public void Switch_Area(Switch_Area area,float load_time)
    {
        if (area.has_animation)
            area.door.Play("Open");
        current_area = area;
        Invoke(nameof(Load_building),load_time);
    }
    public void Switch_Area(string area_name,float load_time)
    {
        if (area_name!="Overworld")
        {
            Switch_Area a = find_area(area_name);
            if (a.has_animation)
                a.door.Play("Open");
            current_area = a;
            Invoke(nameof(Load_building), load_time);
        }
        else
            To_Over_world();
    }
    public Switch_Area find_area(string area_name)
    {
        foreach (Switch_Area a in Areas)
            if (a.area_name == area_name)
                return a;
        return null;
    }
    public void To_Over_world()
    {
        if (area_building != null)
        {
            area_building.interior.SetActive(false);
            area_building.inside_area = false;
            Player_movement.instance.transform.position = area_building.door_pos.localPosition;
            Player_movement.instance.canmove = false;
        }
        else
            Player_movement.instance.transform.position = Game_Load.instance.player_data.player_Position;
        foreach (Switch_Area a in Areas)
            a.overworld.SetActive(true);
        if (area_building != null)
            if (area_building.has_animation)
                area_building.door.Play("Close");
        Invoke(nameof(reset_movement), 1f);
        current_area = find_area("Overworld");
        Player_movement.instance.can_use_bike = true;
        Game_Load.instance.player_data.Location = current_area.area_name;
        area_building = null;
    }
    void Load_building()
    {
        foreach (Switch_Area a in Areas)
            a.overworld.SetActive(false);
        overworld_actions.instance.doing_action = false;
        Player_movement.instance.canmove = false;
        current_area.inside_area = true;
        current_area.interior.SetActive(true);
        area_building = current_area;
        if(!save_area)
            Player_movement.instance.transform.position = current_area.Mat_pos.position;
        Invoke(nameof(reset_movement), 1f);
        Game_Load.instance.player_data.Location = current_area.area_name;
    }
    void reset_movement()
    {
        Player_movement.instance.canmove = true;
        save_area = false;
    }
}
