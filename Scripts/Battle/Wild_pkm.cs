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
    public Battle_Participant currentEnemyParticipant;
    public bool inBattle = false;
    public bool ranAway = false;
    public bool canAttack = true;
    public static Wild_pkm Instance;
    [SerializeField]private bool _usedMove = false;
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
        TargetPlayer(0);//attack player, since its single battle
        if(Utility.RandomRange(1,11)>3)//70% chance
            DetermineMoveChoice();
        else
            RunAway();
    }

    private void RunAway()
    {
        if(!participant.canEscape) return;
        Dialogue_handler.Instance.DisplayBattleInfo(participant.pokemon.pokemonName+" ran away");
        Battle_handler.Instance.EndBattle(false);
        inBattle = false;
        ranAway = true;
    }
    private void TargetPlayer(int selectedIndex)
    {
        Battle_handler.Instance.currentEnemyIndex = selectedIndex;
    }
    private void DetermineMoveChoice()
    {
        if(Utility.RandomRange(1,11)<5)//40% chance
            UseRandom();
        else
        {
            if (Utility.RandomRange(1, 11) < 8)
            {
                var effectiveMove = GetEffectiveMove();
                if (effectiveMove == null)
                    UseStrongestMove();
                else
                    UseMove(effectiveMove);
            }
            else
                UseStrongestMove();
        }
    }
    
    private void UseStrongestMove()
    {
        List<Move> validMoves = new();
        foreach (var move in participant.pokemon.moveSet)
        {//look for all non-immune moves
            if (!BattleOperations.CheckImmunity(currentEnemyParticipant.pokemon, move.type)) 
                validMoves.Add(move);
        }
        if (validMoves.Count > 0)
        {
            var strongestMove = validMoves.OrderByDescending(p => p.moveDamage).ToList();
            UseMove(strongestMove[0]);
        }
        else
            UseRandom();
    }

    private void UseRandom()
    {
        var randMove = Utility.RandomRange(0, participant.pokemon.moveSet.Count);
        UseMove(participant.pokemon.moveSet[randMove]);
    }

    private void UseMove(Move move)
    {
        Battle_handler.Instance.UseMove(move,participant);
        _usedMove = true;
    }

    private Move GetEffectiveMove()
    {
        foreach (var move in participant.pokemon.moveSet)
        {//look for super effective attacking move
            var effectiveness = BattleOperations.GetTypeEffectiveness(currentEnemyParticipant, move.type);
            if ( effectiveness < 2 || move.isBuffOrDebuff) continue;
            return move;
        }
        return null;
    }
}
