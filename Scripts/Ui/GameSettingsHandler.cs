using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
public enum GameSettingName{TextSpeed,BattleStyle}
public class GameSettingsHandler : MonoBehaviour,IInjectable
{
    public List<GameSetting> gameSettings = new();
    public List<GameObject> gameSettingsHeading = new();
    public GameObject mainUI;
    public GameObject whiteSelector;
    public GameSetting currentSetting { get; private set; }
    [SerializeField]private List<SettingsConfig> settingConfigs = new();
    private readonly Dictionary<GameSettingName, Action<int>> _settingsMethods = new ();
    
    private Save_manager _saveDataHandler;
    private Dialogue_handler _dialogueHandler;
    private Battle_handler _battleHandler;
    
    public void Inject(ServiceContainer container)
    {
        _saveDataHandler = container.Resolve<Save_manager>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
    {
        _settingsMethods.Add(GameSettingName.TextSpeed,_dialogueHandler.SetTextSpeed);
        _settingsMethods.Add(GameSettingName.BattleStyle,_battleHandler.SetBattleStyle);
        
        if(Application.platform == RuntimePlatform.WebGLPlayer)
        {
            _saveDataHandler.OnUploadedDataReady += SetSetting;
        }
        else
        {
            SetSetting();
        }
    }

    private void SetSetting()
    {
        var savedSettings = _saveDataHandler.LoadGameSettingsData();
        settingConfigs.Clear();
        if (savedSettings.Count > 0)
        {
            settingConfigs.AddRange(savedSettings);
        }
        else
        {
            foreach (var setting in gameSettings)
            {
                //set defaults
                settingConfigs.Add(new(0,setting.settingOptions.Count-1,setting.gameSettingName));
            }
        }
        foreach (var config in settingConfigs)
        {
            currentSetting = gameSettings.First(s=>s.gameSettingName == config.settingName);
            SetOptionTextColor(config.currentIndex);
            _settingsMethods[config.settingName].Invoke(config.currentIndex);
        }
        SetCurrentSetting(0);
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
        yield return null;
    }
}
