using System;
using System.Collections;
using UnityEngine;

public class WildPokemonAiHandler : MonoBehaviour,IInjectable
{
    public BattleParticipant participant;

    private Action _currentBehaviorAction;
    private BattleAiBehaviorMode _behaviorMode;
    
    private BattleHandler _battleHandler;
    
    public void Inject(ServiceContainer container)
    {
        _battleHandler = container.Resolve<BattleHandler>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _battleHandler.OnBattleEnd += ()=>
        {
            _currentBehaviorAction = null;
            _behaviorMode = BattleAiBehaviorMode.Natural;
        };
    }

    public void SetBehavior(BattleAiBehaviorMode behaviorMode)
    {
        _behaviorMode = behaviorMode;
    }
    public void AssignBehaviorAction(Action action)
    {
        _currentBehaviorAction = action;
    }
    
    public void MakeBattleDecision()
    {
        if (_behaviorMode == BattleAiBehaviorMode.Controlled)
        {
            _currentBehaviorAction?.Invoke();
            return;
        }
        if(Utility.RandomChance(CommonRandom.Rnd30) || participant.canEscape)
        {
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
