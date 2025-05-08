using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
public class Game_Load : MonoBehaviour
{
    public Button load_btn;
    public Button newGame_btn;
    public GameObject new_player_ui; 
    public GameObject Start_ui;
    public InputField name_input;
    private readonly int _maxNameLength = 14;
    private readonly int _minNameLength = 4;
    public Transform Start_house_pos;
    public GameObject world_Map;
    public Player_data playerData;
    [SerializeField]private Player_movement playerMovement;//for initial game load
    public static Game_Load Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Start_ui.SetActive(true);
        world_Map.SetActive(false);
        playerMovement.gameObject.SetActive(false);
        load_btn.gameObject.SetActive(true);
        newGame_btn.gameObject.SetActive(true);
    }

    private void LoadNewPlayerPage()
    {
        load_btn.gameObject.SetActive(false);
        newGame_btn.gameObject.SetActive(false);
        new_player_ui.SetActive(true);
    }
    public void PreventGameLoad()
    {
        load_btn.interactable = false;
        newGame_btn.interactable = true;
    }
    public void CreateNewPlayer()
    {
        if (name_input.text.Length < _maxNameLength && name_input.text.Length > _minNameLength-1)
        {
            var playerName = name_input.text;
            var data = ScriptableObject.CreateInstance<Player_data>();
            data.Player_name = playerName;
            data.player_Money = 300;
            data.NumBadges = 0;
            data.Trainer_ID = Utility.Random16Bit();
            data.Secret_ID = Utility.Random16Bit();
            data.player_Position = Start_house_pos.position;
            data.Location = "Player House";
            playerData = data;
            Dialogue_handler.instance.Write_Info("Welcome "+playerName,"Details",1f);
            StartGame();
        }
        else
        {
            Dialogue_handler.instance.Write_Info("Name must be between 4 and 14 characters","Details",1.5f);
        }
    }
    public void ExitGame()
    {
        Application.Quit();
    }
    public void NewGame()
    {
        Save_manager.instance.Erase_save();
        LoadNewPlayerPage();
    }
    public void StartGame()
    {
        new_player_ui.SetActive(false);
        Start_ui.SetActive(false);
        playerMovement.gameObject.SetActive(true);
        playerMovement.transform.position = playerData.player_Position;
        world_Map.SetActive(true);
        Area_manager.Instance.loadingPLayerFromSave = true;
        Area_manager.Instance.SwitchToArea(playerData.Location,0f);
    }
}

