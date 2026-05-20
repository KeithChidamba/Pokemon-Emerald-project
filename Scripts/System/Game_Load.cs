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
    public GameObject loadButton;
    public GameObject newGameButton;
    public GameObject uploadButton;
    public GameObject createNewPlayerUi;
    public GameObject menuSelector;
    public GameObject menuUiParent;
    [SerializeField] private Image _loadingScreen;
    [SerializeField] private Camera startMenuCam;
    public InputField name_input;
    private readonly int _maxNameLength = 14;
    private readonly int _minNameLength = 4;
    public GameObject world_Map;
    public PlayerData playerData;
    public bool LoadedFromSave { 
        get;
        private set;
    }
    private bool _saveDataExists;
    
    public event Action OnGameStarted;
    private SaveDataHandler _saveHandler;
    private Dialogue_handler _dialogueHandler;
    private Area_manager _areaHandler;
    private Player_movement _playerMovement;
    private overworld_actions _overworldActions;
    private Bag _playerBagHandler;
    private InputStateHandler _inputStateHandler;
    
    public void Inject(ServiceContainer container)
    {
        _playerBagHandler = container.Resolve<Bag>();
        _saveHandler = container.Resolve<SaveDataHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _areaHandler = container.Resolve<Area_manager>();
        _playerMovement = container.Resolve<Player_movement>();
        _overworldActions = container.Resolve<overworld_actions>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
    {
        menuUiParent.SetActive(true);
        loadButton.SetActive(true);
        newGameButton.gameObject.SetActive(true);
        LoadedFromSave = true;
        _saveDataExists = true;
        var isWebgl = Application.platform == RuntimePlatform.WebGLPlayer;
        uploadButton.SetActive(isWebgl);
        
        var menuSelectables = new List<SelectableUI>
        {
            new(loadButton, StartGame, !isWebgl),
            new(newGameButton, NewGame, true)
        };
        if (isWebgl)
        {
            menuSelectables.Add(new(uploadButton, _saveHandler.UploadSaveZip, true));
        }
        _inputStateHandler.ChangeInputState(new (InputStateName.StartMenu,
            InputStateGroup.None,false,
            menuUiParent, InputDirection.Vertical, menuSelectables,
            menuSelector,true,true,canExit:false));
    }


    private void LoadNewPlayerPage()
    {
        uploadButton.gameObject.SetActive(false);
        loadButton.gameObject.SetActive(false);
        newGameButton.gameObject.SetActive(false);
        createNewPlayerUi.SetActive(true);
    }
    
    public void PreventGameLoad()
    {
        _saveDataExists = false;
        _inputStateHandler.currentState.selectableUis[0].canBeSelected = false;
        _inputStateHandler.currentState.selectableUis[1].canBeSelected = true;
    }
    public void AllowGameLoad()
    {
        _inputStateHandler.currentState.selectableUis[0].canBeSelected = true;
        _inputStateHandler.currentState.selectableUis[1].canBeSelected = false;
    }
    public void CreateNewPlayer()//button
    {
        if (name_input.text.Length < _maxNameLength && name_input.text.Length > _minNameLength - 1)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                StartCoroutine(_saveHandler.CreateDefaultWebglDirectories());
            }

            var playerName = name_input.text;
            var data = ScriptableObject.CreateInstance<PlayerData>();
            data.playerName = playerName;
            data.playerMoney = 300;
            data.numBadges = 0;
            data.trainerID = Utility.Random16Bit();
            data.secretID = Utility.Random16Bit();
            data.location = AreaName.PlayerGarden;
            var gardenLocation = _areaHandler.overworldAreas.First(a => a.data.areaName == AreaName.PlayerGarden);
            data.playerPosition = gardenLocation.tileLocation;
            playerData = data;
            LoadedFromSave = false;
            StartGame();
        }
        else
        {
            _dialogueHandler.DisplayDetails("Name must be between 4 and 14 characters");
        }
    }
    private void NewGame()//button
    {
        if (_saveDataExists)
        {
            _dialogueHandler.DisplayCustomOptions($"Save data detected!, Are you sure you want to erase it?",
                new[] { "Yes", "No" }, new Action[] { StartNewGame, null });
        }

        void StartNewGame()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                _saveHandler.EraseSaveData();
            _dialogueHandler.EndDialogue();
            LoadNewPlayerPage();
        }
    }

    private IEnumerator GameStartLoading()
    {
        _inputStateHandler.ResetRelevantUi(InputStateName.StartMenu);
        _overworldActions.EquipItem(_playerBagHandler.SearchForItem(playerData.equippedItemName));
        _dialogueHandler.EndDialogue();
        OnGameStarted?.Invoke();
        menuUiParent.SetActive(false);
        createNewPlayerUi.SetActive(false);
        _loadingScreen.gameObject.SetActive(true);
        Color startColor = new Color(255, 255f, 255f,0);
        Color endColor = Color.white;
        float elapsed = 0f;
        var duration = 1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _loadingScreen.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        //give everything time to load
        yield return new WaitUntil(()=>elapsed >= duration);
        _loadingScreen.gameObject.SetActive(false);
        startMenuCam.gameObject.SetActive(false);
        world_Map.SetActive(true);
        _playerMovement.ActivatePlayerFromSave(playerData.playerPosition);
        _areaHandler.LoadAreaFromSave(playerData.location);
    }
    private void StartGame()//button
    {
        StartCoroutine(GameStartLoading());
    }
}

