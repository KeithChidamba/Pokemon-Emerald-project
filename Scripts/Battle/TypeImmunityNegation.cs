using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TypeImmunityNegation
{
    public ImmunityNegationMove moveName;
    public List<Types> ImmunityNegationTypes = new ();
    private Battle_Participant _participant;
    private Battle_Participant _victimOfimmunityNegation;
    public TypeImmunityNegation(ImmunityNegationMove moveNameEnum,Battle_Participant participant
        , Battle_Participant victim)
    {
        _participant =  participant;
        moveName = moveNameEnum;
        _victimOfimmunityNegation = victim;
    }
    public void RemoveNegationOnSwitchOut(Battle_Participant participant)
    {
        if (participant != _participant) return;
        Battle_handler.Instance.OnSwitchOut -= RemoveNegationOnSwitchOut;
        _victimOfimmunityNegation.immunityNegations.RemoveAll(n => n.moveName == moveName);
    }
}

public enum ImmunityNegationMove{Foresight}
