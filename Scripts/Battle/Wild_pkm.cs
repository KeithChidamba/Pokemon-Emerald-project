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
            while(!Used_move)
                choose_move();
        }
    }

    private void choose_move()
    {
        if(Utility.Get_rand(1,11)<5)//40% chance
        { //random move
            int rand_move = Utility.Get_rand(0, pokemon.num_moves);
            Battle_handler.instance.Use_Move(pokemon.move_set[rand_move],pokemon);
            Used_move = true;
            Debug.Log("random");
        }
        else
        {
            if (Utility.Get_rand(1, 11) < 8)
            {
                Debug.Log("effective");
                for (int i = 0; i < pokemon.num_moves; i++)
                {//look for super effective attacking move
                    float eff = Utility.TypeEffectiveness(Enemy_pokemon, pokemon.move_set[i].type);
                    Debug.Log("eff: "+eff);
                    if ( eff > 1 && !pokemon.move_set[i].is_Buff_Debuff) 
                    {
                        Battle_handler.instance.Use_Move(pokemon.move_set[i], pokemon);
                        Used_move = true;
                        
                        break;
                    }
                    if (i == pokemon.num_moves - 1)
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
        Debug.Log("strongest");
        for (int i = 0; i < pokemon.num_moves; i++) //check for null break point
        {
            if (!Utility.isImmuneTo(Enemy_pokemon, pokemon.move_set[i].type)) //look for all non-immune moves
            {
                mock_moveset.Add(pokemon.move_set[i]);
            }
            if (i==pokemon.num_moves-1)
            {
                strongest_move = mock_moveset.OrderByDescending(p => p.Move_damage).ToList();
                Battle_handler.instance.Use_Move( strongest_move[0],pokemon);
                Used_move = true;
                
            }
        }
    }
}
