using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class TeamEffects
{
    public List<Barrier> barriers = new();
    public List<StatChangeabilityData> statChangeEffects = new();
    public BattleTeam battleTeam;
    
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    
    public TeamEffects(ServiceContainer container,BattleTeam team)
    {
        battleTeam = team;
        
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        
        _turnBasedCombatHandler.OnTurnsCompleted += CheckStatChangeImmunityDuration;
        _turnBasedCombatHandler.OnTurnsCompleted += CheckBarrierDuration;
    }
    
    public void ClearEffects()
    {
        barriers.Clear();
        statChangeEffects.Clear();
    }
    
    public void GetStatChangeImmunity(StatChangeability changeability,int numTurns)
    {
        var existingProtection = statChangeEffects
            .FirstOrDefault(s => s.changeability == changeability);
        if (existingProtection is not null)
        {
            existingProtection.effectDuration = numTurns;
            return;
        }
        statChangeEffects.Add(new StatChangeabilityData(changeability,numTurns));
    }
    private void CheckStatChangeImmunityDuration()
    {
        if (statChangeEffects.Count==0) return;
        foreach (var protection in statChangeEffects)
        {
            protection.effectDuration--;
        }
        statChangeEffects.RemoveAll(s => s.effectDuration < 1);
    }
    private void CheckBarrierDuration()
    {
        if (barriers.Count == 0) return;

        foreach (var barrier in barriers)
        {
            barrier.barrierDuration--;
        }
        barriers.RemoveAll(b => b.barrierDuration < 1);
    }
}
