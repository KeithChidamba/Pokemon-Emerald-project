using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]
public struct SettingConfig
{
    public GameSettingName settingName;
    public int currentIndex;
    public int maxIndex;

    public SettingConfig(GameSettingName settingName, int currentIndex, int maxIndex)
    {
        this.settingName = settingName;
        this.currentIndex = currentIndex;
        this.maxIndex = maxIndex;
    }

    public void SetIndex(int newIndex)
    {
        currentIndex = Mathf.Clamp(currentIndex+newIndex, 0, maxIndex);
    }
}
public enum GameSettingName{TextSpeed,BattleScene,BattleStyle}
public class GameSettingsHandler : MonoBehaviour,IInjectable
{
    public List<GameSetting> gameSettings = new();
    public List<GameObject> gameSettingsHeading = new();
    public GameObject mainUI;
    public GameObject whiteSelector;
    public GameSetting currentSetting { get; private set; }
    [SerializeField]private List<SettingConfig> settingConfigs = new();
    
    
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
    public void SetCurrentOption(int newOptionIndex)
    {
        var findIndex = settingConfigs.FindIndex(setting=>setting.settingName == currentSetting.gameSettingName);
        var data = settingConfigs[findIndex];
        data.SetIndex(newOptionIndex);
       
        var newConfig = new SettingConfig(data.settingName, data.currentIndex, data.maxIndex);
        settingConfigs.RemoveAt(findIndex);
        settingConfigs.Add(newConfig);
    }
    public void ReflectChangedSetting(int newOptionIndex)
    {
        var findIndex = settingConfigs.FindIndex(setting=>setting.settingName == currentSetting.gameSettingName);
        var data = settingConfigs[findIndex];
        data.currentIndex = newOptionIndex;
        
        var newConfig = new SettingConfig(data.settingName, data.currentIndex, data.maxIndex);
        settingConfigs.RemoveAt(findIndex);
        settingConfigs.Add(newConfig);

        //ignore these comments
        //logic to make setting changes here
        //on game load, set these setting again
    }
    public void Inject(Container container)
    {
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
    {
        //testing at the moment
        settingConfigs.Add(new(GameSettingName.TextSpeed,1,2));
        settingConfigs.Add(new(GameSettingName.BattleScene,0,1));
        settingConfigs.Add(new(GameSettingName.BattleStyle,0,1));
        
        for (int i = 0; i< gameSettings.Count; i++)
        {
            currentSetting = gameSettings[i];
            SetOptionTextColor(settingConfigs[i].currentIndex);
        }
        SetCurrentSetting(0);
       
    }
}
