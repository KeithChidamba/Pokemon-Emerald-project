using System;

[Serializable]
public class DamageDisplayData
{
    public BattleParticipant affectedParticipant;
    public bool displayEffectiveness;
    public float healthChange;
    public DamageSource damageSource;
    public float effectivenessScore;
    
    public DamageDisplayData(DamageSource damageSource,BattleParticipant affectedParticipant,
        bool displayEffectiveness = true,
        float healthChange = 0,
        float effectivenessScore = 1f)
    {
        this.affectedParticipant = affectedParticipant;
        this.damageSource = damageSource;
        this.effectivenessScore = effectivenessScore;
        this.displayEffectiveness = displayEffectiveness;
        this.healthChange = healthChange;
    }

}
