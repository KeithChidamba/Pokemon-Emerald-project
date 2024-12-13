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
        if (command._turn.attacker_.canAttack)
        {
            if (!Move_successful(command._turn.attacker_))
                Dialogue_handler.instance.Write_Info(command._turn.attacker_+" missed the attack","Details");
            else
                return true;
        }
        else
        {
            if (command._turn.attacker_.isFlinched)
                Dialogue_handler.instance.Write_Info(command._turn.attacker_+" flinched!","Details");
            else
                //freeze,paralysis
                Dialogue_handler.instance.Write_Info(command._turn.attacker_+" is "+ command._turn.attacker_.Status_effect,"Details");
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
                CancelMove(command);
        }
        Next_turn();
        Reset_Moves();
        yield return null;
    }
    public void CancelMove(Pkm_Use_Move command)
    {
        Move_history.Remove(command);
        command.Undo();
    }
    public void Next_turn()
    {
        //check on pokemon status,health etc
        Battle_handler.instance.Doing_move = false;
        if ( Battle_handler.instance.isDouble_battle)
            Change_turn(4,1);
        else
            Change_turn(3,2);
    }
    public void Change_turn(int participant_index,int step)
    {
        if (Current_pkm_turn < participant_index-1)
        {
            Current_pkm_turn+=step;
        }
        else
        {
            Battle_handler.instance.View_options();
            Current_pkm_turn = 0;
            Battle_handler.instance.Battle_P[Current_pkm_turn].Selected_Enemy = false;//allow player to attack
        }
        OnNewTurn?.Invoke();
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
        speed_list = Move_history.OrderByDescending(p => p._turn.attacker_.speed).ToList();
        foreach (Pkm_Use_Move command in speed_list)
        {
            if (command._turn.move_.Priority == "First")
            {
                priority_list.Add(command);
            }
        }
        foreach (Pkm_Use_Move command in speed_list)
        {
            if (command._turn.move_.Priority != "First")
            {
                priority_list.Add(command);
            }
        }
        return priority_list;
    }
}
