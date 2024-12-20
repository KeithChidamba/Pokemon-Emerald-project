using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class Game_Load : MonoBehaviour
{
    public Button load_btn;
    public Button newGame_btn;
    public GameObject new_player_ui; 
    public GameObject Start_ui;
    public InputField name_input;
    private int max_name_len = 14;
    private int min_name_len = 4;
    public Transform Start_house_pos;
    public GameObject world_Map;
    public Player_data player_data;
    [SerializeField]private Player_movement player_movement;//for initial game load
    public static Game_Load instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        Start_ui.SetActive(true);
        world_Map.SetActive(false);
        player_movement.gameObject.SetActive(false);
        load_btn.gameObject.SetActive(true);
        newGame_btn.gameObject.SetActive(true);
    }

    public void New_player_page()
    {
        load_btn.gameObject.SetActive(false);
        newGame_btn.gameObject.SetActive(false);
        new_player_ui.SetActive(true);
    }
    public void New_Player_Data()
    {
        load_btn.interactable = false;
        newGame_btn.interactable = true;
    }
    public void Create_Player()
    {
        if (name_input.text.Length < max_name_len && name_input.text.Length > min_name_len-1)
        {
            string player_name = name_input.text;
            Player_data pl_data = ScriptableObject.CreateInstance<Player_data>();
            pl_data.Player_name = player_name;
            pl_data.player_Money = 300;
            pl_data.Trainer_ID = pl_data.Generate_ID();
            pl_data.player_Position = Start_house_pos.position;
            pl_data.Location = "Player House";
            player_data = pl_data;
            Dialogue_handler.instance.Write_Info("Welcome "+player_name,"Details");
            Dialogue_handler.instance.Dialouge_off(1f);
            Start_Game();
        }
        else
        {
            Dialogue_handler.instance.Write_Info("Name must be between 4 and 14 characters","Details");
            Dialogue_handler.instance.Dialouge_off(1.5f);
        }
    }
    public void Exit_game()
    {
        Application.Quit();
    }
    public void New_game()
    {
        Save_manager.instance.Erase_save();
        New_player_page();
    }
    public void Start_Game()
    {
        new_player_ui.SetActive(false);
        Start_ui.SetActive(false);
        player_movement.gameObject.SetActive(true);
        player_movement.transform.position = player_data.player_Position;
        world_Map.SetActive(true);
        Area_manager.instance.save_area = true;
        Area_manager.instance.Switch_Area(player_data.Location,0f);
    }
}

