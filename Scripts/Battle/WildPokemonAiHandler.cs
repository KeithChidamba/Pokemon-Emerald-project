using System.Collections;
using UnityEngine;

public class WildPokemonAiHandler : MonoBehaviour,IInjectable
{
    public Battle_Participant participant;
    [SerializeField]private bool inBattle;
    
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Battle_handler _battleHandler;

    
    public void Inject(ServiceContainer container)
    {
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
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
        if (_battleHandler.battleParticipants[_turnBasedCombatHandler.CurrentTurnIndex].pokemon.pokemonID
            != participant.pokemon.pokemonID)
        {
            return;
        }
        if (Utility.RandomRange(1, 11) > 3 || !participant.canEscape)
        {
            var randMove = Utility.RandomRange(0, participant.pokemon.moveSet.Count);
            //attack player, since its single battle
            _battleHandler.UseMove(participant.pokemon.moveSet[randMove],participant,0);
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
