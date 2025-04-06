using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Abilities : MonoBehaviour
{
    public Battle_Participant participant;
    
    public void Set_ability()
    {
        string pkm_ability = participant.pokemon.ability.abilityName.ToLower().Replace(" ","");
        Debug.Log(pkm_ability);
        Dictionary<string, Action> AbilityMethods = new Dictionary<string, Action>
        {
                {"blaze",blaze},
                {"guts",guts},
                {"levitate",levitate},
                {"overgrow",overgrow},
                {"torrent",torrent},
                {"paralysiscombo",paralysiscombo},
                {"sandpit",sandpit},
                {"static",static_},        //underscore because some ability names are c# keywords
                {"shedskin",shedskin},
                {"swarm",swarm}
        };
        if (AbilityMethods.TryGetValue(pkm_ability, out Action ability))
        {
            //if(abi)
            //{participant.OnAbilityUsed += ability;}
        }
        else
        {
            // Not found
            Console.WriteLine($"Ability '{pkm_ability}' not found!");
        }

    }

    public void ResetState()
    {
        
    }
    void blaze()
    {
            
    }
    void guts()
    {
        if (participant.pokemon.HP < (participant.pokemon.max_HP * 0.33f))
        {
            BuffDebuffData buffData = new BuffDebuffData(participant.pokemon, "Attack", true, 1);
            Move_handler.instance.GiveBuff_Debuff(buffData);
        }
    }
    void levitate()
    {
        
    }
    void overgrow()
    {
        
    }
    void paralysiscombo()
    {
        
    }
    void sandpit()
    {
        
    }
    void shedskin()
    {
        
    }
    void static_()
    {
        
    }
    void swarm()
    {
        
    }
    void torrent()
    {
        
    }
}
