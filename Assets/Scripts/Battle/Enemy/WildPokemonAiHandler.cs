using System;
using System.Collections;
using UnityEngine;

public class WildPokemonAiHandler : MonoBehaviour,IInjectable
{
    public BattleParticipant participant;
    [SerializeField]private bool inBattle;
    
    private Action _currentBehaviorAction;
    private BattleAiBehaviorMode _behaviorMode;
    
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private BattleHandler _battleHandler;

    
    public void Inject(ServiceContainer container)
    {
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _turnBasedCombatHandler.OnNewTurn += MakeBattleDecision;
        _battleHandler.OnBattleEnd += ()=>
        {
            _currentBehaviorAction = null;
            _behaviorMode = BattleAiBehaviorMode.Natural;
            inBattle = false;
        };
    }

    public void SetBattleState()
    {
        inBattle = true;
    }
    public void SetBehavior(BattleAiBehaviorMode behaviorMode)
    {
        _behaviorMode = behaviorMode;
    }
    public void AssignBehaviorAction(Action action)
    {
        _currentBehaviorAction = action;
    }
    
    private void MakeBattleDecision()
    {
        if (!inBattle) return;
        //check if its pokemon's turn
        if (_battleHandler.GetCurrentParticipant().participantKey != participant.participantKey)
        {
            return;
        }
        if (_behaviorMode == BattleAiBehaviorMode.Controlled)
        {
            _currentBehaviorAction?.Invoke();
            return;
        }
        if(Utility.RandomChance(CommonRandom.Rnd30) || participant.canEscape)
        {
            inBattle = false;
            _battleHandler.EndBattle(BattleEndState.PokemonRanAway);
        }
        else
        {
            var randMove = Utility.RandomRange(0, participant.pokemon.moveSet.Count);
            //attack player, since its single battle
            _battleHandler.UseMove(participant.pokemon.moveSet[randMove],participant,BattleParticipantKey.Player);
        }
    }
    public IEnumerator EndWildBattle()
    {
        _battleHandler.EndBattle(BattleEndState.PlayerWon);
        yield return null;
    }
}
