using System;

[Serializable]
public class DamageBuffAbilityData
{
    private float _damageBuffMultiplier;
    public Func<BattleParticipant, BattleParticipant, Move, bool> conditionForBuff;

    public DamageBuffAbilityData(
        float damageBuffMultiplier,
        Func<BattleParticipant, BattleParticipant, Move, bool> conditionForBuff)
    {
        _damageBuffMultiplier = damageBuffMultiplier;
        this.conditionForBuff = conditionForBuff;
    }

    public float CanBuffDamage(
        BattleParticipant attacker,
        BattleParticipant victim,
        Move move)
    {
        if (conditionForBuff.Invoke(attacker, victim, move))
            return _damageBuffMultiplier;
        return 1f;
    }
}

public class DamageBuff
{
    public AbilityName abilityName;
    public PokemonType type;
    public float multiplier;

    public DamageBuff(AbilityName abilityName,float multiplier,PokemonType type=PokemonType.Typeless)
    {
        this.abilityName = abilityName;
        this.multiplier = multiplier;
        this.type = type;
    }
}