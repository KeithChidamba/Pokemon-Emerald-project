using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Wild_pkm : MonoBehaviour
{
    public Pokemon pokemon;
    public Pokemon Enemy_pokemon;
    public bool InBattle = false;
    public static Wild_pkm instance;
    [SerializeField]private bool Used_move = false;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    private void Start()
    {
        Turn_Based_Combat.instance.OnNewTurn += Reset_move;
    }
    private void OnDestroy()
    {
        Turn_Based_Combat.instance.OnNewTurn -= Reset_move;//chek for continuoues moves like rollout
    }
    void Reset_move()
    {
        Used_move = false;
    }
    private void Update()
    {
        if (!InBattle) return;
            Make_Decision();
    }
    private void Make_Decision()
    {
        //check if its pokemon's turn
        if (Battle_handler.instance.Battle_P[Turn_Based_Combat.instance.Current_pkm_turn].pokemon == pokemon && !Used_move)
        {
            Battle_handler.instance.Select_player();//attack player, since its single battle
            choose_move();
        }
    }

    private void choose_move()
    {
        if(Utility.Get_rand(1,11)<5)//40% chance
            Use_random();
        else
        {
            if (Utility.Get_rand(1, 11) < 8)
            {
                if (CanUse_effective());
                else
                {
                    UseStrongest_move();
                }
            }
            else
            {
                UseStrongest_move();
            }
        }
    }
    
    private void UseStrongest_move()
    {
        List<Move> strongest_move = new();
        List<Move> mock_moveset = new();
        foreach (Move m in pokemon.move_set)
        {
            if (!Utility.isImmuneTo(Enemy_pokemon, m.type)) //look for all non-immune moves
                mock_moveset.Add(m);
        }
        if (mock_moveset.Count > 0)
        {
            strongest_move = mock_moveset.OrderByDescending(p => p.Move_damage).ToList();
            use_move(strongest_move[0]);
        }
        else
            Use_random();
    }

    void Use_random()
    {
        int rand_move = Utility.Get_rand(0, pokemon.move_set.Count);
        use_move(pokemon.move_set[rand_move]);
    }

    void use_move(Move move)
    {
        Battle_handler.instance.Use_Move(move,pokemon);
        Used_move = true;
    }
    bool CanUse_effective()
    {
        foreach (Move m in pokemon.move_set)
        {//look for super effective attacking move
            float eff = Utility.TypeEffectiveness(Enemy_pokemon, m.type);
            if ( eff > 1 && !m.is_Buff_Debuff)
            {
                use_move(m);
                return true;
            }
        }
        return false;
    }
}
