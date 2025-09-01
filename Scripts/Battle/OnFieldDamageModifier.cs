using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class OnFieldDamageModifier
{
    public float damageModifier;
    public PokemonOperations.Types typeAffected;
    private Battle_Participant _participant;
    public bool removeOnSwitch;
    public OnFieldDamageModifier(float damageModifier, PokemonOperations.Types typeAffected
        ,Battle_Participant user = null,bool removeOnSwitch = true)
    {
        this.damageModifier = damageModifier;
        this.typeAffected = typeAffected;
        _participant = user;
        this.removeOnSwitch = removeOnSwitch;
    }
    public void RemoveOnSwitchOut(Battle_Participant participant)
    {
        if(!removeOnSwitch)return;
        if (participant != _participant) return;
        Battle_handler.Instance.OnSwitchOut -= RemoveOnSwitchOut;
        Move_handler.Instance.RemoveFieldDamageModifier(typeAffected);
    }
    public void RemoveAfterWeather()
    {
        Turn_Based_Combat.Instance.OnWeatherEnd -= RemoveAfterWeather;
        Move_handler.Instance.RemoveFieldDamageModifier(typeAffected);
    }
}
