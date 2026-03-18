using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class OnFieldDamageModifier
{
    public DamageModifierInfo modifierInfo;
    private Battle_Participant _participant;
    public bool removeOnSwitch;
    private Battle_handler _battleHandler;
    private Move_handler _moveUsageHandler;
    private Turn_Based_Combat _turnBasedHandler;
    
    public OnFieldDamageModifier(Battle_handler battleHandler,Move_handler moveUsageHandler,Turn_Based_Combat turnBasedHandler,DamageModifierInfo info
        ,Battle_Participant user = null,bool removeOnSwitch = true)
    {
        _turnBasedHandler = turnBasedHandler;
        _battleHandler = battleHandler;
        _moveUsageHandler = moveUsageHandler;
        modifierInfo = info;
        _participant = user;
        this.removeOnSwitch = removeOnSwitch;
    }
    public void RemoveOnSwitchOut(Battle_Participant participant)
    {
        if(!removeOnSwitch)return;
        if (participant != _participant) return;
        _battleHandler.OnSwitchOut -= RemoveOnSwitchOut;
        _moveUsageHandler.RemoveFieldDamageModifier(modifierInfo.typeAffected);
    }
    public void RemoveAfterWeather()
    {
        _turnBasedHandler.OnWeatherEnd -= RemoveAfterWeather;
        _moveUsageHandler.RemoveFieldDamageModifier(modifierInfo.typeAffected);
    }
}
