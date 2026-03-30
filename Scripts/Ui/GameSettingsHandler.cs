using System.Collections.Generic;
using UnityEngine;

public enum GameSettingName{TextSpeed,BattleScene,BattleStyle}
public class GameSettingsHandler : MonoBehaviour,IInjectable
{
    public List<GameSetting> gameSettings = new();
    
    public void Inject(Container container)
    {
        
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
    {
        
    }

}
