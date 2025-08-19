
public class DamageDisplayData
{
    public Battle_Participant affectedParticipant;
    public bool displayEffectiveness;
    public bool isSpecificDamage;
    public float predefinedHealthChange;
    public DamageDisplayData(Battle_Participant affectedParticipant, bool displayEffectiveness = true,
        bool isSpecificDamage = false, float predefinedHealthChange = 0)
    {
        this.affectedParticipant = affectedParticipant;
        this.displayEffectiveness = displayEffectiveness;
        this.isSpecificDamage = isSpecificDamage;
        this.predefinedHealthChange = predefinedHealthChange;
    }

}
