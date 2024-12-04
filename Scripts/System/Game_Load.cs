using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Game_Load : MonoBehaviour
{
    public Save_manager saves;
    public Options_manager options;
    public Button load_btn;
    public Button newGame_btn;
    public GameObject new_player_ui; 
    public GameObject Start_ui;
    public InputField name_input;
    private int max_name_len = 14;
    private int min_name_len = 4;
    public Dialogue_handler dialogue;
    public Transform Start_house_pos;
    public GameObject world_Map;
    public Area_manager area;
    private void Start()
    {
        Start_ui.SetActive(true);
        world_Map.SetActive(false);
        options.player.movement.gameObject.SetActive(false);
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
            pl_data.player_Money = 100;
            pl_data.Player_ID = saves.storage.Generate_ID(pl_data.Player_name);
            pl_data.player_Position = Start_house_pos.position;
            pl_data.Location = "Player House";
            options.player_data = pl_data;
            dialogue.Write_Info("Welcome "+player_name,"Details");
            dialogue.Dialouge_off(1f);
            Start_Game();
        }
        else
        {
            dialogue.Write_Info("Name must be between 4 and 14 characters","Details");
            dialogue.Dialouge_off(1.5f);
        }
    }
    public void Exit_game()
    {
        Application.Quit();
    }
    public void New_game()
    {
        saves.Erase_save();
        New_player_page();
    }
    public void Start_Game()
    {
        new_player_ui.SetActive(false);
        Start_ui.SetActive(false);
        options.player.movement.transform.position = options.player_data.player_Position;
        options.player.movement.gameObject.SetActive(true);
        world_Map.SetActive(true);
        area.save_area = true;
        area.Switch_Area(options.player_data.Location,0f);
    }
}

