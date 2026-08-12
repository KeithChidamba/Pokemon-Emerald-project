using System.Collections;
using UnityEngine;

public class WildPokemonAiHandler : MonoBehaviour,IInjectable
{
    public BattleParticipant participant;
    [SerializeField]private bool inBattle;
    
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
        _battleHandler.OnBattleEnd += ()=>inBattle = false;
    }
    public void SetBattleState()
    {
        inBattle = true;
    }
    private void MakeBattleDecision()
    {
        if (!inBattle) return;
        //check if its pokemon's turn
        if (_battleHandler.GetCurrentParticipant().participantKey != participant.participantKey)
        {
            return;
        }
        if (Utility.RandomChance(CommonRandom.Rnd70) || !participant.canEscape)
        {
            var randMove = Utility.RandomRange(0, participant.pokemon.moveSet.Count);
            //attack player, since its single battle
            _battleHandler.UseMove(participant.pokemon.moveSet[randMove],participant,BattleParticipantKey.Player);
        }
        else
        {
            inBattle = false;
            _battleHandler.EndBattle(BattleEndState.PokemonRanAway,null);
        }
    }
    public IEnumerator EndWildBattle()
    {
        _battleHandler.EndBattle(BattleEndState.PlayerWon, null);
        yield return null;
    }
    
    
}
