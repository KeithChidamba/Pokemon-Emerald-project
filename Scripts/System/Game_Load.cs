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
   public Button uploadButton;
    public GameObject new_player_ui; 
    public GameObject Start_ui;
    public InputField name_input;
    private readonly int _maxNameLength = 14;
    private readonly int _minNameLength = 4;
    public Transform Start_house_pos;
    public GameObject world_Map;
    public Player_data playerData;
    public static Game_Load Instance;
    public event Action OnGameStarted; 
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
        Player_movement.Instance.playerObject.SetActive(false);
        load_btn.gameObject.SetActive(true);
        newGame_btn.gameObject.SetActive(true);
    }

    private void LoadNewPlayerPage()
    {
        uploadButton.gameObject.SetActive(false);
        load_btn.gameObject.SetActive(false);
        newGame_btn.gameObject.SetActive(false);
        new_player_ui.SetActive(true);
    }
    public void PreventGameLoad()
    {
        load_btn.interactable = false;
        newGame_btn.interactable = true;
    }
    public void AllowGameLoad()
    {
        load_btn.interactable = true;
        newGame_btn.interactable = false;
    }
    public void CreateNewPlayer()
    {
        if (name_input.text.Length < _maxNameLength && name_input.text.Length > _minNameLength-1)
        {
            Save_manager.Instance.CreateDefaultWebglDirectories();
            var playerName = name_input.text;
            var data = ScriptableObject.CreateInstance<Player_data>();
            data.playerName = playerName;
            data.playerMoney = 300;
            data.numBadges = 0;
            data.trainerID = Utility.Random16Bit();
            data.secretID = Utility.Random16Bit();
            data.playerPosition = Start_house_pos.position;
            data.location = AreaData.AreaName.PlayerHouse;
            playerData = data;
            StartGame();
        }
        else
        {
            Dialogue_handler.Instance.DisplayDetails("Name must be between 4 and 14 characters",1.5f);
        }
    }
    public void NewGame()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            Save_manager.Instance.EraseSaveData();
        Dialogue_handler.Instance.EndDialogue();
        LoadNewPlayerPage();
    }
    public void StartGame()
    {
        Dialogue_handler.Instance.EndDialogue();
        new_player_ui.SetActive(false);
        Start_ui.SetActive(false);
        Player_movement.Instance.playerObject.SetActive(true);
        Player_movement.Instance.playerObject.transform.position = playerData.playerPosition;
        overworld_actions.Instance.EquipItem(Bag.Instance.SearchForItem(playerData.equippedItemName));
        world_Map.SetActive(true);
        Area_manager.Instance.loadingPlayerFromSave = true;
        Area_manager.Instance.SwitchToArea(playerData.location);
        OnGameStarted?.Invoke();
    }
}

