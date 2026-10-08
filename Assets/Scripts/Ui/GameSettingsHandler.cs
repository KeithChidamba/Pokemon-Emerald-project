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
public enum GameSettingName{TextSpeed,BattleStyle}
public class GameSettingsHandler : MonoBehaviour,IInjectable
{
    public List<GameSetting> gameSettings = new();
    public List<GameObject> gameSettingsHeading = new();
    public GameObject mainUI;
    public GameObject whiteSelector;
    public GameSetting currentSetting;
    [SerializeField]private List<SettingsConfig> settingConfigs = new();
    private readonly Dictionary<GameSettingName, Action<int>> _settingsMethods = new ();
    
    private SaveDataHandler _saveDataHandler;
    private DialogueHandler _dialogueHandler;
    private BattleHandler _battleHandler;
    
    public void Inject(ServiceContainer container)
    {
        _saveDataHandler = container.Resolve<SaveDataHandler>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _settingsMethods.Add(GameSettingName.TextSpeed,_dialogueHandler.SetTextSpeed);
        _settingsMethods.Add(GameSettingName.BattleStyle,_battleHandler.SetBattleStyle);
    }
    public void LoadDefaultState()
    {
        settingConfigs.Clear();
        foreach (var setting in gameSettings)
        {
            //set defaults
            settingConfigs.Add(new(0,setting.settingOptions.Count-1,setting.gameSettingName));
        }
        
        foreach (var config in settingConfigs)
        {
            currentSetting = gameSettings.First(s=>s.gameSettingName == config.settingName);
            SetOptionTextColor(config.currentIndex);
        }
        SetCurrentSetting(0);
    }
    public void ConfigureSavedSettings()
    {
        GetSavedSettings();
        
        foreach (var config in settingConfigs)
        {
            currentSetting = gameSettings.First(s=>s.gameSettingName == config.settingName);
            SetOptionTextColor(config.currentIndex);
            _settingsMethods[config.settingName].Invoke(config.currentIndex);
        }
        SetCurrentSetting(0);
    }
    private void GetSavedSettings()
    {
        var savedSettings = _saveDataHandler.GetSavedGameSettingsData();
        settingConfigs.Clear();
        settingConfigs.AddRange(savedSettings);
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
            _saveDataHandler.SaveDataAsJson(config,config.settingName.ToString(),SaveDataDirectory.GameSettings);
        }
        yield return null;
    }
}
