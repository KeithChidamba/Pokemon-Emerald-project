using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abilities : MonoBehaviour
{
    public Battle_Participant participant;
    [SerializeField] private string pkm_ability;
    private void Update()
    {
        if (Options_manager.instance.playerInBattle)
        {
            Invoke(pkm_ability,0f);
        }
    }
    public void Set_ability()
    { 
        //underscore because some ability names are c# keywords
        pkm_ability = Utility.removeSpace(participant.pokemon.ability.ability.ToLower())+"_";
    }
    void inferno_()
    {

    }
    void guts_()
    {
        
    }
    void levitate_()
    {
        
    }
    void overgrow_()
    {
        
    }
    void paralysiscombo_()
    {
        
    }
    void regular_()
    {
        
    }
    void sandpit_()
    {
        
    }
    void shedskin_()
    {
        
    }
    void static_()
    {
        
    }
    void swarm_()
    {
        
    }
    void torrent_()
    {
        
    }
}
