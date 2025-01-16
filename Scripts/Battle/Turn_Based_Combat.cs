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
        //Debug.Log(turn.attacker_.name);
        if(turn.attacker_.pokemon.HP<=0) return false;
        if (!turn.victim_.is_active)
        {
            Dialogue_handler.instance.Battle_Info(turn.attacker_.pokemon.Pokemon_name+" missed the attack");
            return false;
        }
        if (turn.attacker_.pokemon.canAttack)
        {
            if (turn.move_.Move_accuracy < 100)//not a sure-hit move
            {
                if (!MoveSuccessfull(turn))
                {
                    if(turn.attacker_.pokemon.Accuracy > turn.victim_.pokemon.Evasion)
                        Dialogue_handler.instance.Battle_Info(turn.attacker_.pokemon.Pokemon_name+" missed the attack");
                    else
                        Dialogue_handler.instance.Battle_Info(turn.victim_.pokemon.Pokemon_name+" dodged the attack");
                }
                else
                    return true;
            }else
                return true;
        }
        else
        {
            if (turn.attacker_.pokemon.isFlinched)
                Dialogue_handler.instance.Battle_Info(turn.attacker_.pokemon.Pokemon_name+" flinched!");
            else if(turn.attacker_.pokemon.Status_effect!="None")
                Dialogue_handler.instance.Battle_Info(turn.attacker_.pokemon.Pokemon_name+" is affected by "+ turn.attacker_.pokemon.Status_effect);
        }
        return false;
    }
    IEnumerator ExecuteMoves(List<Turn> TurnOrder)
    {
        foreach (Turn CurrentTurn in new List<Turn>(TurnOrder) )
        {
            OnMoveExecute?.Invoke();
            yield return new WaitUntil(()=>!Dialogue_handler.instance.messagesLoading);
            if(CurrentTurn.attacker_.pokemon==null)continue;
            if(CurrentTurn.victim_.pokemon==null)continue;
            if (Can_Attack(CurrentTurn))
            {
                //Debug.Log(CurrentTurn.attacker_.pokemon.Pokemon_name + "'s turn");
                //CurrentTurn.victim_.pokemon = Battle_handler.instance.Battle_P[Current_pkm_turn].pokemon;
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
    public void CancelTurn(Turn turn)
    {
        //Debug.Log(turn.attacker_.pokemon.Pokemon_name + "'s turn removed completely");
        Turn_history.Remove(turn);
    }

    public Turn SearchForTurn(Battle_Participant participant)
    {
        Debug.Log(participant.pokemon.Pokemon_name + "'s turn being removed");
        List<Turn> Turns = Turn_history.ToList();
        Turns.RemoveAll(turn => turn.attacker_.pokemon.Pokemon_ID != participant.pokemon.Pokemon_ID);
        if(Turn_history.Count == 0)return null;
        
        return Turns[0];
    }

    public void RemoveTurn(Battle_Participant participant)
    {
        Turn_history.RemoveAll(turn => turn.attacker_.pokemon.Pokemon_ID == participant.pokemon.Pokemon_ID
                                       | turn.victim_.pokemon.Pokemon_ID == participant.pokemon.Pokemon_ID );
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
                           (turn.attacker_.pokemon.Accuracy / turn.victim_.pokemon.Evasion);
        if (Hit_Chance>rand)
            return true;
        return false;
    }
    private List<Turn> Set_priority()
    {
        List<Turn> speed_list = new();
        List<Turn> priority_list = new();
        speed_list = Turn_history.OrderByDescending(p => p.attacker_.pokemon.speed).ToList();
        priority_list = speed_list.OrderByDescending(p => p.move_.Priority).ToList();
        return priority_list;
    }
}
