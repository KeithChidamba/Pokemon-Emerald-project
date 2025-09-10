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
    public OnFieldDamageModifier(DamageModifierInfo info
        ,Battle_Participant user = null,bool removeOnSwitch = true)
    {
        modifierInfo = info;
        _participant = user;
        this.removeOnSwitch = removeOnSwitch;
    }
    public void RemoveOnSwitchOut(Battle_Participant participant)
    {
        if(!removeOnSwitch)return;
        if (participant != _participant) return;
        Battle_handler.Instance.OnSwitchOut -= RemoveOnSwitchOut;
        Move_handler.Instance.RemoveFieldDamageModifier(modifierInfo.typeAffected);
    }
    public void RemoveAfterWeather()
    {
        Turn_Based_Combat.Instance.OnWeatherEnd -= RemoveAfterWeather;
        Move_handler.Instance.RemoveFieldDamageModifier(modifierInfo.typeAffected);
    }
}
