using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Turn_Based_Combat : MonoBehaviour
{
    public static Turn_Based_Combat instance; 
    [SerializeField]List<Turn> Turn_history = new();
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
    public void SaveMove(Turn turn)
    {
        Turn_history.Add(turn);
        //Debug.Log(Battle_handler.instance.Participant_count+" part count");
       // Debug.Log(turn._turn.attacker_.pokemon.Pokemon_name + " turn added lv" +turn._turn.attacker_.pokemon.Current_level);
        if( (Battle_handler.instance.isDouble_battle && Current_pkm_turn == Battle_handler.instance.Participant_count-1 )
         || (Current_pkm_turn == Battle_handler.instance.Participant_count ))
            StartCoroutine(ExecuteMoves(Set_priority()));
        else
            Next_turn();
    }
    void Reset_Moves()
    {
        Turn_history.Clear();
        StopAllCoroutines();
    }
    bool Can_Attack(Turn turn)
    {
        Battle_Participant attacker_=Battle_handler.instance.Battle_P[turn.attackerIndex];
        Battle_Participant victim_=Battle_handler.instance.Battle_P[turn.victimIndex];
        if(attacker_.pokemon.HP<=0) return false;
        if (!victim_.is_active)
        {
            Dialogue_handler.instance.Battle_Info(attacker_.pokemon.Pokemon_name+" missed the attack");
            return false;
        }
        if (attacker_.pokemon.canAttack)
        {
            if (turn.move_.Move_accuracy < 100)//not a sure-hit move
            {
                if (!MoveSuccessfull(turn))
                {
                    if(attacker_.pokemon.Accuracy > victim_.pokemon.Evasion)
                        Dialogue_handler.instance.Battle_Info(attacker_.pokemon.Pokemon_name+" missed the attack");
                    else
                        Dialogue_handler.instance.Battle_Info(victim_.pokemon.Pokemon_name+" dodged the attack");
                }
                else
                    return true;
            }else
                return true;
        }
        else
        {
            if (attacker_.pokemon.isFlinched)
                Dialogue_handler.instance.Battle_Info(attacker_.pokemon.Pokemon_name+" flinched!");
            else if(attacker_.pokemon.Status_effect!="None")
                Dialogue_handler.instance.Battle_Info(attacker_.pokemon.Pokemon_name+" is affected by "+ attacker_.pokemon.Status_effect);
        }
        return false;
    }
    IEnumerator ExecuteMoves(List<Turn> TurnOrder)
    {
        foreach (Turn CurrentTurn in TurnOrder)
        {
            if (!CheckParticipantState(CurrentTurn)) continue;
            OnMoveExecute?.Invoke();
            yield return new WaitUntil(()=>!Dialogue_handler.instance.messagesLoading);
            if (Can_Attack(CurrentTurn))
            {
                //Debug.Log(CurrentTurn.attacker_.pokemon.Pokemon_name + "'s turn");
                Move_handler.instance.Doing_move = true;
                Move_handler.instance.Do_move(CurrentTurn);
                yield return new WaitUntil(() => !Move_handler.instance.Doing_move);
                CancelTurn(CurrentTurn);
            }
            else
            {
                //Debug.Log(CurrentTurn.attacker_.pokemon.Pokemon_name + "'s turn cancelled");
                CancelTurn(CurrentTurn);
                yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
            }
        }
        //Debug.Log("moves over");
        OnTurnEnd?.Invoke();
        yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
        Battle_handler.instance.Reset_move();
        Next_turn();
        yield return null;
    }

    bool CheckParticipantState(Turn turn)
    {
        if (Battle_handler.instance.Battle_P[turn.attackerIndex].is_active & Battle_handler.instance.Battle_P[turn.victimIndex].is_active)
            if (!Battle_handler.instance.Battle_P[turn.attackerIndex].fainted & !Battle_handler.instance.Battle_P[turn.victimIndex].fainted)
                if (Battle_handler.instance.Battle_P[turn.attackerIndex].pokemon != null &
                    Battle_handler.instance.Battle_P[turn.attackerIndex].pokemon != null)
                    return true;
        if(Battle_handler.instance.Battle_P[turn.attackerIndex].pokemon != null)
            Dialogue_handler.instance.Battle_Info(Battle_handler.instance.Battle_P[turn.attackerIndex].pokemon.Pokemon_name+" missed!");
        return false;
    } 
    void CancelTurn(Turn turn)
    {
        Turn_history.Remove(turn);
    }

    public void RemoveTurn(Battle_Participant participant)
    {
        Turn_history.RemoveAll(turn => Battle_handler.instance.Battle_P[turn.attackerIndex].pokemon.Pokemon_ID == participant.pokemon.Pokemon_ID);
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
    private bool MoveSuccessfull(Turn turn)
    {
        int rand = Utility.Get_rand(1, 100);
        float Hit_Chance = turn.move_.Move_accuracy *
                           (Battle_handler.instance.Battle_P[turn.attackerIndex].pokemon.Accuracy / 
                            Battle_handler.instance.Battle_P[turn.victimIndex].pokemon.Evasion);
        if (Hit_Chance>rand)
            return true;
        return false;
    }
    private List<Turn> Set_priority()
    {
        List<Turn> speed_list = new();
        List<Turn> priority_list = new();
        speed_list = Turn_history.OrderByDescending(p => Battle_handler.instance.Battle_P[p.attackerIndex].pokemon.speed).ToList();
        priority_list = speed_list.OrderByDescending(p => p.move_.Priority).ToList();
        return priority_list;
    }
}
