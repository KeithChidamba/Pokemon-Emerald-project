using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        if (statChangeEffects.Any(s => s.changeability == changeability))
        {
            Debug.LogWarning("added duplicate stat change effect");
        };
        statChangeEffects.Add(new StatChangeabilityData(changeability,numTurns));
    }
    private void CheckStatChangeImmunityDuration()
    {
        if (statChangeEffects.Count==0) return;
        
        statChangeEffects.ForEach(s=>s.effectDuration--);
        statChangeEffects.RemoveAll(s => s.effectDuration == 0);
    }
    private void CheckBarrierDuration()
    {
        if (barriers.Count == 0) return;

        foreach (var barrier in barriers)
        {
            barrier.barrierDuration--;
        }
        barriers.RemoveAll(b => b.barrierDuration == 0);
    }
}
