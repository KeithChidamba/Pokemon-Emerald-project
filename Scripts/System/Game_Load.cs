using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
public class Game_Load : MonoBehaviour,IInjectable
{
    
    public Button load_btn;
    public Button newGame_btn;
    public Button uploadButton;
    public GameObject new_player_ui; 
    public GameObject Start_ui;
    public InputField name_input;
    private readonly int _maxNameLength = 14;
    private readonly int _minNameLength = 4;
    public GameObject world_Map;
    public Player_data playerData;
    public GameObject mobileControlsUI;
    private bool _isMobile;
    public event Action OnGameStarted;
    private Save_manager _saveHandler;
    private Dialogue_handler _dialogueHandler;
    private Area_manager _areaHandler;
    private Player_movement _playerMovement;
    private overworld_actions _overworldActions;
    private Bag _playerBagHandler;
    
    [DllImport("__Internal")] private static extern void CheckIfMobileBrowser();

    public bool IsMobile()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            return Application.isMobilePlatform;
        }
        return _isMobile;
    }
    
    public void Inject(ServiceContainer container)
    {
        _playerBagHandler = container.Resolve<Bag>();
        _saveHandler = container.Resolve<Save_manager>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _areaHandler = container.Resolve<Area_manager>();
        _playerMovement = container.Resolve<Player_movement>();
        _overworldActions = container.Resolve<overworld_actions>();
        
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
    {
        Debug.Log("checking");
        CheckIfMobileBrowser();
        Start_ui.SetActive(true);
        load_btn.gameObject.SetActive(true);
        newGame_btn.gameObject.SetActive(true);
    }
    
    public void ConfirmIsMobile()
    {
        Debug.Log("con mobile");
        _isMobile = true;
        mobileControlsUI.SetActive(true);
    }
    public void DenyIsMobile()
    {
        Debug.LogError("deny mobile");
        _isMobile = false;
        mobileControlsUI.SetActive(false);
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
            _saveHandler.CreateDefaultWebglDirectories();
            var playerName = name_input.text;
            var data = ScriptableObject.CreateInstance<Player_data>();
            data.playerName = playerName;
            data.playerMoney = 300;
            data.numBadges = 0;
            data.trainerID = Utility.Random16Bit();
            data.secretID = Utility.Random16Bit();
            data.location = AreaName.PlayerGarden;
            data.playerPosition =  new Vector3(-5, -20, 0);
            playerData = data;
            StartGame();
        }
        else
        {
            _dialogueHandler.DisplayDetails("Name must be between 4 and 14 characters");
        }
    }
    public void NewGame()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            _saveHandler.EraseSaveData();
        _dialogueHandler.EndDialogue();
        LoadNewPlayerPage();
    }

    private void StartGame()
    {
        _dialogueHandler.EndDialogue();
        new_player_ui.SetActive(false);
        Start_ui.SetActive(false);
        
        _overworldActions.EquipItem(_playerBagHandler.SearchForItem(playerData.equippedItemName));
        world_Map.SetActive(true);
        
        _playerMovement.ActivatePlayerFromSave(playerData.playerPosition);
        
        _areaHandler.loadingPlayerFromSave = true;
        _areaHandler.SwitchToArea(playerData.location);
        
        OnGameStarted?.Invoke();
    }
}

