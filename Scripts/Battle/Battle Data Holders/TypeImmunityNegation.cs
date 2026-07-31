using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TypeImmunityNegation
{
    public LearnSetMoveName moveName;
    public List<PokemonType> ImmunityNegationTypes = new ();
    private BattleParticipant _participant;
    private BattleParticipant _victimOfImmunityNegation;
    private BattleHandler _battleHandler;
    
    public TypeImmunityNegation(BattleHandler battleHandler,LearnSetMoveName moveNameEnum,BattleParticipant participant
        , BattleParticipant victim)
    {
        _battleHandler = battleHandler;
        _participant =  participant;
        moveName = moveNameEnum;
        _victimOfImmunityNegation = victim;
    }
    public void RemoveNegationOnSwitchOut(BattleParticipant swapParticipant)
    {
        if (swapParticipant.participantKey == _victimOfImmunityNegation.participantKey
            || swapParticipant.participantKey == _participant.participantKey)
        {
            _battleHandler.OnSwitchOut -= RemoveNegationOnSwitchOut;
            _victimOfImmunityNegation.immunityNegations.RemoveAll(n => n.moveName == moveName);
        }
    }
}

