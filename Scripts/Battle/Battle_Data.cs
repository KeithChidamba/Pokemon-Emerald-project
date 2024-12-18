using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Battle_Data
{
    //float speed
    public static void Reset_Battle_state(Battle_Participant participant)
    {
        
        participant.pokemon.canAttack = true;
        participant.pokemon.CanBeDamaged = true;
        participant.pokemon.isFlinched = false;
        participant.pokemon.Buff_Debuff = "None";
        participant.pokemon = null;
        //save stats that debuffed
    }
}
