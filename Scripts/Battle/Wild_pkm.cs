using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

public class Wild_pkm : MonoBehaviour
{
    public Battle_Participant participant;
    public Battle_Participant currentEnemyPokemon;
    public bool inBattle = false;
    public bool ranAway = false;
    public bool canAttack = true;
    public static Wild_pkm Instance;
    private bool _usedMove = false;
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
            choose_move();
        else
            RunAway();
        
    }

    private void RunAway()
    {
        if(!participant.canEscape)
        {
            Dialogue_handler.Instance.Battle_Info(participant.pokemon.Pokemon_name +" is trapped");
            return;
        }
        Dialogue_handler.Instance.Battle_Info(participant.pokemon.Pokemon_name+" ran away");
        Battle_handler.Instance.End_Battle(false);
        inBattle = false;
        ranAway = true;
    }
    private void TargetPlayer(int selectedIndex)
    {
        //enemy choosing player
        participant.enemySelected = true;
        Battle_handler.Instance.currentEnemyIndex = selectedIndex;
    }
    private void choose_move()
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
        foreach (Move m in participant.pokemon.move_set)
        {//look for all non-immune moves
            if (!BattleOperations.CheckImmunity(currentEnemyPokemon.pokemon, m.type)) 
                validMoves.Add(m);
        }
        if (validMoves.Count > 0)
        {
            var strongestMove = validMoves.OrderByDescending(p => p.Move_damage).ToList();
            UseMove(strongestMove[0]);
        }
        else
            UseRandom();
    }

    private void UseRandom()
    {
        var randMove = Utility.RandomRange(0, participant.pokemon.move_set.Count);
        UseMove(participant.pokemon.move_set[randMove]);
    }

    private void UseMove(Move move)
    {
        Battle_handler.Instance.UseMove(move,participant);
        _usedMove = true;
    }
    private Move GetEffectiveMove()
    {
        foreach (Move move in participant.pokemon.move_set)
        {//look for super effective attacking move
            var effectiveness = BattleOperations.GetTypeEffectiveness(currentEnemyPokemon, move.type);
            if ( effectiveness < 2 || move.is_Buff_Debuff) continue;
            return move;
        }
        return null;
    }
}
