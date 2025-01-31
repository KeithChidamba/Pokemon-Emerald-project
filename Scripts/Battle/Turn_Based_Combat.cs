using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class Turn_Based_Combat : MonoBehaviour
{
    public static Turn_Based_Combat instance; 
    [SerializeField]List<Turn> Turn_history = new();
    public event Action OnNewTurn;
    public event Action OnMoveExecute;
    public event Action OnTurnEnd;
    public int Current_pkm_turn = 0;
    public bool LevelEventDelay = false;
    public bool FainEventDelay = false;
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
        if( (Battle_handler.instance.isDouble_battle && isLastParticipant())
         || (Current_pkm_turn == Battle_handler.instance.Participant_count ))
            StartCoroutine(ExecuteMoves(Set_priority()));
        else
            Next_turn();
    }

    bool isLastParticipant()
    {
        List <Battle_Participant> activeParticipants = new();
        activeParticipants = Battle_handler.instance.Battle_Participants.ToList();
        activeParticipants.RemoveAll(participant => participant.pokemon==null);
        if (activeParticipants.Last() ==
            Battle_handler.instance.Battle_Participants[Current_pkm_turn])
            return true;
        return false;
    }
    void Reset_Moves()
    {
        Turn_history.Clear();
        FainEventDelay = false;
        LevelEventDelay = false;
        StopAllCoroutines();
    }
    bool Can_Attack(Turn turn, Battle_Participant attacker_,Battle_Participant victim_)
    {
        if(attacker_.pokemon.HP<=0) return false;
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
        foreach (Turn CurrentTurn in TurnOrder )
        {
            Battle_Participant attacker_=Battle_handler.instance.Battle_Participants[CurrentTurn.attackerIndex];
            Battle_Participant victim_=Battle_handler.instance.Battle_Participants[CurrentTurn.victimIndex];
            if (!CheckParticipantState(attacker_))
                continue;
            if (!isValidParticipant(CurrentTurn,attacker_))
                continue;
            if (!CheckParticipantState(victim_))
            {//if attack was directed at a pokemon that just fainted
                Dialogue_handler.instance.Battle_Info(attacker_.pokemon.Pokemon_name+" missed the attack");
                yield return new WaitUntil(()=>!Dialogue_handler.instance.messagesLoading);
                continue;
            }
            OnMoveExecute?.Invoke();
            yield return new WaitUntil(()=>!Dialogue_handler.instance.messagesLoading);
            if (Can_Attack(CurrentTurn,attacker_,victim_))
            {
                Move_handler.instance.Doing_move = true;
                if(attacker_.previousMove.Split('/')[0] == CurrentTurn.move_.Move_name)
                    attacker_.previousMove = CurrentTurn.move_.Move_name + 
                                             (int.Parse(attacker_.previousMove.Split('/')[1])+1);
                else
                    attacker_.previousMove = CurrentTurn.move_.Move_name + "1";
                Move_handler.instance.Do_move(CurrentTurn);
                yield return new WaitUntil(() => !Move_handler.instance.Doing_move);
                yield return new WaitForSeconds(0.5f);
                yield return new WaitUntil(() => !LevelEventDelay);
                yield return new WaitUntil(() => !FainEventDelay);
            }
            else
                yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
        }
        yield return new WaitUntil(() => !FainEventDelay);
        yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
        Turn_history.Clear();
        OnTurnEnd?.Invoke();
        yield return new WaitUntil(()=> !Dialogue_handler.instance.messagesLoading);
        Battle_handler.instance.Reset_move();
        Next_turn();
        yield return null;
    }
    
    bool CheckParticipantState(Battle_Participant participant)
    {
        if (participant.is_active)
            if (!participant.fainted)
                if (participant.pokemon != null)
                    return true;
        return false;
    }

    bool isValidParticipant(Turn turn,Battle_Participant participant)
    {
        if (turn.attackerID == participant.pokemon.Pokemon_ID.ToString()|
            turn.victimID == participant.pokemon.Pokemon_ID.ToString())
            return true;
        return false;
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
            Battle_handler.instance.Battle_Participants[Current_pkm_turn].Selected_Enemy = false;//allow player to attack
        }
        OnNewTurn?.Invoke();
        Battle_handler.instance.Doing_move = false;
        if (!Battle_handler.instance.Battle_Participants[Current_pkm_turn].is_active)
            Next_turn();
    }
    private bool MoveSuccessfull(Turn turn)
    {
        int rand = Utility.Get_rand(1, 100);
        float Hit_Chance = turn.move_.Move_accuracy *
                           (Battle_handler.instance.Battle_Participants[turn.attackerIndex].pokemon.Accuracy / 
                            Battle_handler.instance.Battle_Participants[turn.victimIndex].pokemon.Evasion);
        if (Hit_Chance>rand)
            return true;
        return false;
    }
    private List<Turn> Set_priority()
    {
        List<Turn> speed_list = new();
        List<Turn> priority_list = new();
        speed_list = Turn_history.OrderByDescending(p => Battle_handler.instance.Battle_Participants[p.attackerIndex].pokemon.speed).ToList();
        priority_list = speed_list.OrderByDescending(p => p.move_.Priority).ToList();
        return priority_list;
    }
}
