using UnityEngine;

public class Abilities : MonoBehaviour
{
    public Battle_Participant participant;
    public void Set_ability()
    { 
        //underscore because some ability names are c# keywords
        //pkm_ability = participant.pokemon.ability.abilityName.ToLower().Replace(" ", "")+"_";
    }

    void ChangeStats()
    {
        if (participant.pokemon.HP < (participant.pokemon.max_HP * 0.33f))
        {
            BuffDebuffData buffData = new BuffDebuffData(participant.pokemon, "Attack", true, 1);
            Move_handler.instance.GiveBuff_Debuff(buffData);
        }
    }
    void blaze_()
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
