using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

public class Wild_pkm : MonoBehaviour
{
    public Battle_Participant participant;
    public bool inBattle;
    public bool ranAway;
    public static Wild_pkm Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Turn_Based_Combat.Instance.OnNewTurn += MakeBattleDecision;
    }
    
    private void MakeBattleDecision()
    {
        if (!inBattle) return;
        //check if its pokemon's turn
        if (Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex].pokemon.pokemonID
            != participant.pokemon.pokemonID)
        {
            return;
        }
       
        //attack player, since its single battle
        Battle_handler.Instance.currentEnemyIndex = 0;
        
        if (Utility.RandomRange(1, 11) > 3 || !participant.canEscape)
        {
            var randMove = Utility.RandomRange(0, participant.pokemon.moveSet.Count);
            Battle_handler.Instance.UseMove(participant.pokemon.moveSet[randMove],participant);
        }
        else
        {
            inBattle = false;
            ranAway = true;
            Battle_handler.Instance.EndBattle(false);
            Dialogue_handler.Instance.DisplayBattleInfo(participant.pokemon.pokemonName+" ran away");
        }
    }
    public void EndWildBattle()
    {
        inBattle = false;
        Turn_Based_Combat.Instance.faintEventDelay = false;
        Battle_handler.Instance.EndBattle(true);
        Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName +
                                                    " defeated " +participant.pokemon.pokemonName);
    }
    
    
}
