using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SettingsConfig
{
    public GameSettingName settingName;
    public int currentIndex;
    public int maxIndex;

    public SettingsConfig(int currentIndex=0, int maxIndex=0,GameSettingName settingName=0)
    {
        this.settingName = settingName;
        this.currentIndex = currentIndex;
        this.maxIndex = maxIndex;
    }

    public void SetIndex(int change)
    {
        currentIndex = Mathf.Clamp(currentIndex + change, 0, maxIndex);
    }
}
public enum GameSettingName{TextSpeed,BattleStyle,ViewControls}
public class GameSettingsHandler : MonoBehaviour,IInjectable
{
    public List<GameSetting> gameSettings = new();
    public List<GameObject> gameSettingsHeading = new();
    public GameObject mainUI;
    public GameObject whiteSelector;
    public Toggle viewGameControlsToggle;
    public GameSetting currentSetting { get; private set; }
    [SerializeField]private List<SettingsConfig> settingConfigs = new();
    private readonly Dictionary<GameSettingName, Action<int>> _settingsMethods = new ();
    
    private SaveDataHandler _saveDataHandler;
    private Dialogue_handler _dialogueHandler;
    private Battle_handler _battleHandler;
    private InputSourceHandler _inputSourceHandler;
    private Game_Load _gameLoadingHandler;
    
    public void Inject(ServiceContainer container)
    {
        _saveDataHandler = container.Resolve<SaveDataHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _inputSourceHandler = container.Resolve<InputSourceHandler>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
    {
        _settingsMethods.Add(GameSettingName.TextSpeed,_dialogueHandler.SetTextSpeed);
        _settingsMethods.Add(GameSettingName.BattleStyle,_battleHandler.SetBattleStyle);
        viewGameControlsToggle.isOn = false;
        viewGameControlsToggle.onValueChanged.AddListener(OnToggleChanged);
        _gameLoadingHandler.OnGameStarted += DetermineGameSettingsSource;
    }
    
    private void GetSavedSettings()
    {
        var savedSettings = _saveDataHandler.LoadGameSettingsData();
        settingConfigs.Clear();
        settingConfigs.AddRange(savedSettings);
    }

    private void LoadDefaultSettings()
    {
        settingConfigs.Clear();
        foreach (var setting in gameSettings)
        {
            //set defaults
            settingConfigs.Add(new(0,setting.settingOptions.Count-1,setting.gameSettingName));
        }
        settingConfigs.Add(new SettingsConfig(0, 1, GameSettingName.ViewControls));
    }
    private void DetermineGameSettingsSource()
    {
        if (_gameLoadingHandler.LoadedFromSave)
        {
            GetSavedSettings();
        }
        else
        {
            LoadDefaultSettings();
        }
        
        var controlsConfigIndex =
            settingConfigs.FindIndex(setting => setting.settingName == GameSettingName.ViewControls);
        
        viewGameControlsToggle.isOn = settingConfigs[controlsConfigIndex].currentIndex > 0;
        OnToggleChanged(viewGameControlsToggle.isOn);
        settingConfigs.RemoveAt(controlsConfigIndex);
        foreach (var config in settingConfigs)
        {
            currentSetting = gameSettings.First(s=>s.gameSettingName == config.settingName);
            SetOptionTextColor(config.currentIndex);
            _settingsMethods[config.settingName].Invoke(config.currentIndex);
        }
        SetCurrentSetting(0);
    }
    
    private void OnToggleChanged(bool isOn)
    {
        _inputSourceHandler.DisplayMobileControls(isOn);
    }
    
    public void SetOptionTextColor(int optionIndex)
    {
        currentSetting.settingOptions.ForEach(o=>o.color=Color.black);
        var text = currentSetting.settingOptions[optionIndex];
        text.color = Color.red;
    }

    public void SetCurrentSetting(int newIndex)
    {
        currentSetting = gameSettings[newIndex];
    }

    public int GetCurrentOptionIndex()
    {
        return settingConfigs.First(setting=>setting.settingName == currentSetting.gameSettingName).currentIndex;
    }
    public void SetCurrentOption(int optionChangeAmount)
    {
        var data = settingConfigs.First(setting=>setting.settingName == currentSetting.gameSettingName);
        data.SetIndex(optionChangeAmount);
    }
    public void ReflectChangedSetting(int newOptionIndex)
    {
        var data = settingConfigs.First(setting=>setting.settingName == currentSetting.gameSettingName);
        data.currentIndex = newOptionIndex;
        _settingsMethods[data.settingName].Invoke(data.currentIndex);
    }

    public IEnumerator SaveSettings()
    {
        foreach (var config in settingConfigs)
        {
            _saveDataHandler.SaveGameSettingsAsJson(config,config.settingName.ToString());
        }

        var indexFromBool = viewGameControlsToggle.isOn ? 1 : 0;
        
        var viewControlsConfig = new SettingsConfig(indexFromBool, 1, GameSettingName.ViewControls);
        
        _saveDataHandler.SaveGameSettingsAsJson(viewControlsConfig,viewControlsConfig.settingName.ToString());
        yield return null;
    }
}
