using System;

[Serializable]
public class DamageDisplayData
{
    public Pokemon affectedPokemon;
    public Battle_Participant affectedParticipant;
    public bool displayEffectiveness;
    public bool isSpecificDamage;
    public float predefinedHealthChange;
    public DamageDisplayData(Battle_Participant participant=null, bool displayEffectiveness = true,
        bool isSpecificDamage = false, float predefinedHealthChange = 0,Pokemon affectedPokemon = null)
    {
        affectedParticipant = participant;
        
        if(affectedPokemon==null &&  affectedParticipant != null)
            this.affectedPokemon = affectedParticipant.pokemon;
        else
            this.affectedPokemon = affectedPokemon;
        
        this.displayEffectiveness = displayEffectiveness;
        this.isSpecificDamage = isSpecificDamage;
        this.predefinedHealthChange = predefinedHealthChange;
    }

}
