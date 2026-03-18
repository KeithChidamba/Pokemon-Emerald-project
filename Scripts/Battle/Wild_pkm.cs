using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

public class Wild_pkm : MonoBehaviour,IInjectable
{
    public Battle_Participant participant;
    public bool inBattle;
    public bool ranAway;
    
    private Dialogue_handler _dialogueHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Battle_handler _battleHandler;
    private Game_Load _gameLoadingHandler;
    
    public void Inject(Container container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        gameObject.SetActive(true);
        OnInject(); 
    }

    private void OnInject()
    {
        _turnBasedCombatHandler.OnNewTurn += MakeBattleDecision;
    }
    
    private void MakeBattleDecision()
    {
        if (!inBattle) return;
        //check if its pokemon's turn
        if (_battleHandler.battleParticipants[_turnBasedCombatHandler.currentTurnIndex].pokemon.pokemonID
            != participant.pokemon.pokemonID)
        {
            return;
        }
       
        //attack player, since its single battle
        _battleHandler.currentEnemyIndex = 0;
        
        if (Utility.RandomRange(1, 11) > 3 || !participant.canEscape)
        {
            var randMove = Utility.RandomRange(0, participant.pokemon.moveSet.Count);
            _battleHandler.UseMove(participant.pokemon.moveSet[randMove],participant);
        }
        else
        {
            inBattle = false;
            ranAway = true;
            _battleHandler.EndBattle(false);
            _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonName+" ran away");
        }
    }
    public void EndWildBattle()
    {
        inBattle = false;
        _turnBasedCombatHandler.faintEventDelay = false;
        _battleHandler.EndBattle(true);
        _dialogueHandler.DisplayBattleInfo(_gameLoadingHandler.playerData.playerName + " defeated " +participant.pokemon.pokemonName);
    }
    
    
}
