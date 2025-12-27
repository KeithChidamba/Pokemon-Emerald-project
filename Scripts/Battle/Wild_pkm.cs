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
    public bool canAttack = true;
    public static Wild_pkm Instance;
    [SerializeField]private bool _usedMove;
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
        Turn_Based_Combat.Instance.OnNewTurn += ResetMoveUsage;
    }

    public void CanAttack()
    {
        canAttack = true;
    }
    private void ResetMoveUsage()
    {
        _usedMove = false;
    }
    private void Update()
    {
        if (!inBattle) return;
        ranAway = false;
        MakeBattleDecision();
    }
    private void MakeBattleDecision()
    {
        //check if its pokemon's turn
        if (Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex].pokemon
            != participant.pokemon || _usedMove || !canAttack)
            return;
        Battle_handler.Instance.currentEnemyIndex = 0;//attack player, since its single battle
        if (Utility.RandomRange(1, 11) > 3) //70% chance
        {
            var randMove = Utility.RandomRange(0, participant.pokemon.moveSet.Count);
            Battle_handler.Instance.UseMove(participant.pokemon.moveSet[randMove],participant);
            _usedMove = true;
        }
        else
        {
            if(!participant.canEscape) return;
            Dialogue_handler.Instance.DisplayBattleInfo(participant.pokemon.pokemonName+" ran away");
            inBattle = false;
            ranAway = true;
            Battle_handler.Instance.EndBattle(false);
        }
    }
}
