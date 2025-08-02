using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnFieldDamageModifier
{
    public float damageModifier;
    public PokemonOperations.Types typeAffected;
    private Battle_Participant _participant;
    public OnFieldDamageModifier(float damageModifier, PokemonOperations.Types typeAffected,Battle_Participant user)
    {
        this.damageModifier = damageModifier;
        this.typeAffected = typeAffected;
        _participant = user;
    }
    public void RemoveOnSwitchOut(Battle_Participant participant)
    {
        if (participant != _participant) return;
        Battle_handler.Instance.OnSwitchOut -= RemoveOnSwitchOut;
        Move_handler.Instance.RemoveFieldDamageModifier(typeAffected);
    }
}
