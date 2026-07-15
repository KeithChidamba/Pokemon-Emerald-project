using System;

[Serializable]
public class DamageDisplayData
{
    public Pokemon affectedPokemon;
    public Battle_Participant affectedParticipant;
    public bool displayEffectiveness;
    public float healthChange;
    public DamageSource damageSource;
    public float effectivenessScore;
    public DamageDisplayData(DamageSource damageSource,Battle_Participant affectedParticipant=null,
        bool displayEffectiveness = true,
        float healthChange = 0,
        float effectivenessScore = 1f,
        Pokemon affectedPokemon = null)
    {
        this.affectedParticipant = affectedParticipant;
        
        if(affectedPokemon==null && affectedParticipant != null)
            this.affectedPokemon = affectedParticipant.pokemon;
        else
            this.affectedPokemon = affectedPokemon;
        
        this.damageSource = damageSource;
        this.effectivenessScore = effectivenessScore;
        this.displayEffectiveness = displayEffectiveness;
        this.healthChange = healthChange;
    }

}
