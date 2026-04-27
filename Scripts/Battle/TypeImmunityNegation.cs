using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TypeImmunityNegation
{
    public ImmunityNegationMove moveName;
    public List<PokemonType> ImmunityNegationTypes = new ();
    private Battle_Participant _participant;
    private Battle_Participant _victimOfimmunityNegation;
    private Battle_handler _battleHandler;
    public TypeImmunityNegation(Battle_handler battleHandler,ImmunityNegationMove moveNameEnum,Battle_Participant participant
        , Battle_Participant victim)
    {
        _battleHandler = battleHandler;
        _participant =  participant;
        moveName = moveNameEnum;
        _victimOfimmunityNegation = victim;
    }
    public void RemoveNegationOnSwitchOut(Battle_Participant participant)
    {
        if (participant != _participant) return;
        _battleHandler.OnSwitchOut -= RemoveNegationOnSwitchOut;
        _victimOfimmunityNegation.immunityNegations.RemoveAll(n => n.moveName == moveName);
    }
}

public enum ImmunityNegationMove{Foresight}
