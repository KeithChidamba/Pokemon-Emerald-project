using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Turn_Based_Combat : MonoBehaviour
{
    public static Turn_Based_Combat instance; 
    readonly List<Pkm_Use_Move> Move_history = new();
    List<Pkm_Use_Move> speed_list = new();
    List<Pkm_Use_Move> priority_list = new();
    public event Action OnNewTurn;
    public int Current_pkm_turn = 0;
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
        Battle_handler.instance.onBattleEnd += StopAllCoroutines;
    }
    public void SaveMove(Pkm_Use_Move command)
    {
        Move_history.Add(command);
        if (Current_pkm_turn == Battle_handler.instance.Participant_count)
            StartCoroutine(ExecuteMoves(Set_priority()));
        else
            Next_turn();
    }
    void Reset_Moves()
    {
        Move_history.Clear();
    }
    bool Can_Attack(Pkm_Use_Move command)
    {
        if(command._turn.attacker_.pokemon.HP<=0) return false;
        if (command._turn.attacker_.pokemon.canAttack)
        {
            if (command._turn.move_.Move_accuracy < 100)//not a sure-hit move
            {
                if (!Move_successful(command._turn.attacker_.pokemon))
                {
                    Dialogue_handler.instance.Battle_Info(command._turn.attacker_.pokemon+" missed the attack");
                }
                else
                    return true;
            }else
                return true;
        }
        else
        {
            if (command._turn.attacker_.pokemon.isFlinched)
                Dialogue_handler.instance.Battle_Info(command._turn.attacker_.pokemon.Pokemon_name+" flinched!");
            else if(command._turn.attacker_.pokemon.Status_effect!="None")
                Dialogue_handler.instance.Battle_Info(command._turn.attacker_.pokemon.Pokemon_name+" is affected by "+ command._turn.attacker_.pokemon.Status_effect);
        }
        return false;
    }
    IEnumerator ExecuteMoves(List<Pkm_Use_Move> command_order)
    {
        foreach (Pkm_Use_Move command in command_order)
        {
            if (Can_Attack(command))
            {
                command.Execute();
                yield return new WaitUntil(()=> !Move_handler.instance.Doing_move);
            }
            else
            {
                CancelMove(command);
                yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
            }
        }
        Next_turn();
        Reset_Moves();
        yield return null;
    }
    private void CancelMove(Pkm_Use_Move command)
    {
        Move_history.Remove(command);
        command.Undo();
    }
    public void Next_turn()
    {
        if ( Battle_handler.instance.isDouble_battle)
            Change_turn(4,1);
        else
            Change_turn(3,2);
    }
    public void Change_turn(int participant_index,int step)
    {
        if (Current_pkm_turn < participant_index-1)
            Current_pkm_turn+=step;
        else
        {
            Battle_handler.instance.View_options();
            Current_pkm_turn = 0;
            Battle_handler.instance.Battle_P[Current_pkm_turn].Selected_Enemy = false;//allow player to attack
        }
        OnNewTurn?.Invoke();
        Battle_handler.instance.Doing_move = false;
    }
    private bool Move_successful(Pokemon pokemon)
    {
        int rand = Utility.Get_rand(1, 100);
        if (pokemon.Accuracy > rand)
            return true;
        return false;
    }
    private List<Pkm_Use_Move> Set_priority()
    {
        speed_list.Clear();
        priority_list.Clear();
        speed_list = Move_history.OrderByDescending(p => p._turn.attacker_.pokemon.speed).ToList();
        priority_list = speed_list.OrderByDescending(p => p._turn.move_.Priority).ToList();
        return priority_list;
    }
}
