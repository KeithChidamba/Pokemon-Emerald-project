
public class DamageDisplayData
{
    public Pokemon affectedPokemon;
    public Battle_Participant affectedParticipant;
    public bool displayEffectiveness;
    public bool isSpecificDamage;
    public float predefinedHealthChange;
    public DamageDisplayData(Battle_Participant affectedParticipant=null, bool displayEffectiveness = true,
        bool isSpecificDamage = false, float predefinedHealthChange = 0,Pokemon affectedPokemon = null)
    {
        this.affectedParticipant = affectedParticipant;
        this.affectedPokemon = affectedParticipant?.pokemon ?? affectedPokemon;
        this.displayEffectiveness = displayEffectiveness;
        this.isSpecificDamage = isSpecificDamage;
        this.predefinedHealthChange = predefinedHealthChange;
    }

}
