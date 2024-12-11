using System;
using System.Collections;
using System.Collections.Generic;
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
        Turn_Based_Combat.instance.OnNewTurn -= Reset_move;
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
            foreach (Move m in pokemon.move_set)
            {
                if (m != null)
                {
                    if (Enemy_pokemon.isWeakTo(m.type))
                    {
                        Debug.Log("Do move stab");
                        Battle_handler.instance.Use_Move(m,pokemon);
                        Used_move = true;
                    }
                }
                
            }
            /*if(Utility.Get_rand(1,5)==0)
            {
                
            }*/
        }
    }
    
}
