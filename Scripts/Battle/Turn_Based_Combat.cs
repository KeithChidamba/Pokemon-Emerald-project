using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Turn_Based_Combat : MonoBehaviour
{
    public static Turn_Based_Combat instance; 
    [SerializeField]List<Pkm_Use_Move> Move_history = new();
    public event Action OnNewTurn;
    public event Action OnMoveExecute;
    public event Action OnTurnEnd;
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
        Battle_handler.instance.onBattleEnd += Reset_Moves;
    }
    public void SaveMove(Pkm_Use_Move command)
    {
        Move_history.Add(command);
        //Debug.Log(Battle_handler.instance.Participant_count+" part count");
       // Debug.Log(command._turn.attacker_.pokemon.Pokemon_name + " turn added lv" +command._turn.attacker_.pokemon.Current_level);
        if( (Battle_handler.instance.isDouble_battle && Current_pkm_turn == Battle_handler.instance.Participant_count-1 )
         || (Current_pkm_turn == Battle_handler.instance.Participant_count ))
            StartCoroutine(ExecuteMoves(Set_priority()));
        else
            Next_turn();
    }
    void Reset_Moves()
    {
        Move_history.Clear();
        StopAllCoroutines();
    }
    bool Can_Attack(Pkm_Use_Move command)
    {
        if(command._turn.attacker_.pokemon.HP<=0) return false;
        if (!command._turn.victim_.is_active)
        {
            Dialogue_handler.instance.Battle_Info(command._turn.attacker_.pokemon.Pokemon_name+" missed the attack");
            return false;
        }
        if (command._turn.attacker_.pokemon.canAttack)
        {
            if (command._turn.move_.Move_accuracy < 100)//not a sure-hit move
            {
                if (!MoveSuccessfull(command))
                {
                    if(command._turn.attacker_.pokemon.Accuracy > command._turn.victim_.pokemon.Evasion)
                        Dialogue_handler.instance.Battle_Info(command._turn.attacker_.pokemon.Pokemon_name+" missed the attack");
                    else
                        Dialogue_handler.instance.Battle_Info(command._turn.victim_.pokemon.Pokemon_name+" dodged the attack");
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
            OnMoveExecute?.Invoke();
            yield return new WaitUntil(()=>!Dialogue_handler.instance.messagesLoading);
            if (Can_Attack(command))
            {
                //Debug.Log(command._turn.attacker_.pokemon.Pokemon_name + "'s turn");
                command.Execute();
                yield return new WaitUntil(()=> !Move_handler.instance.Doing_move);
                CancelMove(command);
            }
            else
            {
                //Debug.Log(command._turn.attacker_.pokemon.Pokemon_name + "'s turn cancelled");
                CancelMove(command);
                yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
            }
        }
        //Debug.Log("moves over");
        OnTurnEnd?.Invoke();
        yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
        Battle_handler.instance.Reset_move();
        Next_turn();
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
        if (!Battle_handler.instance.Battle_P[Current_pkm_turn].is_active)
            Next_turn();
    }
    private bool MoveSuccessfull(Pkm_Use_Move command)
    {
        int rand = Utility.Get_rand(1, 100);
        float Hit_Chance = command._turn.move_.Move_accuracy *
                           (command._turn.attacker_.pokemon.Accuracy / command._turn.victim_.pokemon.Evasion);
        if (Hit_Chance>rand)
            return true;
        return false;
    }
    private List<Pkm_Use_Move> Set_priority()
    {
        List<Pkm_Use_Move> speed_list = new();
        List<Pkm_Use_Move> priority_list = new();
        speed_list = Move_history.OrderByDescending(p => p._turn.attacker_.pokemon.speed).ToList();
        priority_list = speed_list.OrderByDescending(p => p._turn.move_.Priority).ToList();
        return priority_list;
    }
}
