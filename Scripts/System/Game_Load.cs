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
    public GameObject createPlayerButton;
    public GameObject createNewPlayerUi;
    public GameObject menuSelector;
    public GameObject createNewPlayerUiSelector;
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
    }

    public void OnInject()
    {
        
    }

    public void ShowMenuUI(bool dataExists)
    {
        menuUiParent.SetActive(true);
        newGameButton.gameObject.SetActive(true);
        LoadedFromSave = true;
        _saveDataExists = dataExists;
        var menuSelectables = new List<SelectableUI>();
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            _saveDataExists = false;
            uploadButton.SetActive(true);
            loadButton.SetActive(false);
            menuSelectables.Add(new(uploadButton, _saveHandler.UploadSaveZip, true));
        }
        else
        {
            uploadButton.SetActive(false);
            loadButton.SetActive(_saveDataExists);
            if(_saveDataExists)
            {
                menuSelectables.Add(new(loadButton, StartGame, true));
            }
        }
        menuSelectables.Add(new(newGameButton, NewGame, true));
        
        _inputStateHandler.ChangeInputState(new (InputStateName.StartMenu,
            InputStateGroup.None,false,
            menuUiParent, InputDirection.Vertical, menuSelectables,
            menuSelector,true,true,canExit:false));
    }
    public void PreventGameLoad()
    {
        ShowMenuUI(false);
    }

    private void CreateNewPlayer()
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
            _inputStateHandler.ResetRelevantUi(InputStateName.PlayerCreationMenu,true);
        }
        else
        {
            _dialogueHandler.DisplayDetails("Name must be between 4 and 14 characters");
        }
    }
    private void NewGame()
    {
        if (_saveDataExists)
        {
            _dialogueHandler.DisplayCustomOptions($"Save data detected!, Are you sure you want to erase it?",
                new[] { "Yes", "No" }, new Action[] { LoadPlayerCreationMenu, null });
        }
        else
        {
            LoadPlayerCreationMenu();
        }

        void LoadPlayerCreationMenu()
        {
            // Load New Player Page
            uploadButton.gameObject.SetActive(false);
            loadButton.gameObject.SetActive(false);
            newGameButton.gameObject.SetActive(false);
            menuSelector.SetActive(false);
            createNewPlayerUi.SetActive(true);
            _dialogueHandler.EndDialogue();
            
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                _saveHandler.EraseSaveData();
            }
            var menuSelectables = new List<SelectableUI>
           {
               new(createPlayerButton, CreateNewPlayer, true)
           };
           
           _inputStateHandler.ChangeInputState(new (InputStateName.PlayerCreationMenu,
               InputStateGroup.None,false,
               createNewPlayerUi, InputDirection.Vertical, menuSelectables,
               createNewPlayerUiSelector,true,true,canExit:false));
        }
    }

    private IEnumerator GameStartLoading()
    {
        _inputStateHandler.ResetRelevantUi(InputStateName.StartMenu,true);
        _overworldActions.EquipItem(_playerBagHandler.SearchForItem(playerData.equippedItemName));
        _dialogueHandler.EndDialogue();
        OnGameStarted?.Invoke();
        menuUiParent.SetActive(false);
        createNewPlayerUi.SetActive(false);
        //give everything time to load
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
        yield return new WaitUntil(()=>elapsed >= duration);
        
        _loadingScreen.gameObject.SetActive(false);
        startMenuCam.gameObject.SetActive(false);
        world_Map.SetActive(true);
        _playerMovement.ActivatePlayerFromSave(playerData.playerPosition);
        _areaHandler.LoadAreaFromSave(playerData.location);
    }
    public void StartGame()
    {
        StartCoroutine(GameStartLoading());
    }
}

