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
            //check if have type advantage move that's not immune
            //or check if STAB move that's non-immune 
            //or check highest damage, non-immune move
            // else use random move
            Battle_handler.instance.Select_player();//attack player, since its single battle
            List<Move> strongest_move = new();
            List<Move> mock_moveset = new();
            for (int i = 0;i<pokemon.num_moves;i++)
            {
                if (pokemon.move_set[i] != null)
                {
                    mock_moveset.Add(pokemon.move_set[i]);
                    if (Enemy_pokemon.isWeakTo(pokemon.move_set[i].type) )
                    {
                        Battle_handler.instance.Use_Move(pokemon.move_set[i],pokemon);
                        Used_move = true;
                        break;
                    } 
                    if (i==pokemon.num_moves-1)
                    {
                        strongest_move = mock_moveset.OrderByDescending(p => p.Move_damage).ToList();
                        Battle_handler.instance.Use_Move( strongest_move[0],pokemon);
                        Used_move = true;
                        break;
                    }
                }
            }
            Debug.Log("Do move stab");
            
            /*if(Utility.Get_rand(1,3)<2)//50/50 chance
            {
                
            }*/
        }
    }
    
}
