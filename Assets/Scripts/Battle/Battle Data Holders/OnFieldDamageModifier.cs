using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class OnFieldDamageModifier
{
    public DamageModifierInfo modifierInfo;
    private BattleParticipant _participant;
    public bool removeOnSwitch;
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
    private TurnBasedCombatHandler _turnBasedHandler;
    
    public OnFieldDamageModifier(BattleHandler battleHandler,MoveSequenceHandler moveUsageHandler,TurnBasedCombatHandler turnBasedHandler
        ,DamageModifierInfo info
        ,BattleParticipant user = null,bool removeOnSwitch = true)
    {
        _turnBasedHandler = turnBasedHandler;
        _battleHandler = battleHandler;
        _moveUsageHandler = moveUsageHandler;
        modifierInfo = info;
        _participant = user;
        this.removeOnSwitch = removeOnSwitch;
    }
    public void RemoveOnSwitchOut(BattleParticipant participant)
    {
        if(!removeOnSwitch)return;
        if (participant.participantKey != _participant.participantKey) return;
        _battleHandler.OnSwitchOut -= RemoveOnSwitchOut;
        _moveUsageHandler.RemoveFieldDamageModifier(modifierInfo.modifierSource);
    }
    public void RemoveAfterWeather()
    {
        _turnBasedHandler.OnWeatherEnd -= RemoveAfterWeather;
        _moveUsageHandler.RemoveFieldDamageModifier(modifierInfo.modifierSource);
    }
}
