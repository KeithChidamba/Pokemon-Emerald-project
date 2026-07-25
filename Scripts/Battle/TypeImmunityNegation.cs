using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TypeImmunityNegation
{
    public LearnSetMoveName moveName;
    public List<PokemonType> ImmunityNegationTypes = new ();
    private Battle_Participant _participant;
    private Battle_Participant _victimOfImmunityNegation;
    private Battle_handler _battleHandler;
    
    public TypeImmunityNegation(Battle_handler battleHandler,LearnSetMoveName moveNameEnum,Battle_Participant participant
        , Battle_Participant victim)
    {
        _battleHandler = battleHandler;
        _participant =  participant;
        moveName = moveNameEnum;
        _victimOfImmunityNegation = victim;
    }
    public void RemoveNegationOnSwitchOut(Battle_Participant swapParticipant)
    {
        if (swapParticipant.participantKey == _victimOfImmunityNegation.participantKey
            || swapParticipant.participantKey == _participant.participantKey)
        {
            _battleHandler.OnSwitchOut -= RemoveNegationOnSwitchOut;
            _victimOfImmunityNegation.immunityNegations.RemoveAll(n => n.moveName == moveName);
        }
    }
}

