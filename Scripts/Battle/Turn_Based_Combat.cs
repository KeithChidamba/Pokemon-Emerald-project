using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class Turn_Based_Combat : MonoBehaviour
{
    public static Turn_Based_Combat Instance; 
    List<Turn> _turnHistory = new();
    public event Action OnNewTurn;
    public event Action OnMoveExecute;
    public event Action OnTurnEnd;
    public int currentTurnIndex = 0;
    public bool levelEventDelay = false;
    public bool faintEventDelay = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        Battle_handler.Instance.OnBattleEnd += ResetTurnState;
    }
    public void SaveMove(Turn turn)
    {
        _turnHistory.Add(turn);
        if( (Battle_handler.Instance.isDoubleBattle && IsLastParticipant())
         || (currentTurnIndex == Battle_handler.Instance.participantCount ))
            StartCoroutine(ExecuteMoves(Set_priority()));
        else
            NextTurn();
    }

    private bool IsLastParticipant()
    {
        var livingParticipants = Battle_handler.Instance.battleParticipants.ToList();
        livingParticipants.RemoveAll(participant => participant.pokemon==null);
        if (livingParticipants.Last() ==
            Battle_handler.Instance.battleParticipants[currentTurnIndex])
            return true;
        return false;
    }
    private void ResetTurnState()
    {
        _turnHistory.Clear();
        faintEventDelay = false;
        levelEventDelay = false;
    }
    private bool CanAttack(Turn turn, Battle_Participant attacker,Battle_Participant victim)
    {
        if(attacker.pokemon.HP<=0) return false;
        if (attacker.pokemon.canAttack)
        {
            if (turn.move.Move_accuracy < 100)//not a sure-hit move
            {
                if (!MoveSuccessfull(turn))
                {
                    if(attacker.pokemon.Accuracy >= victim.pokemon.Evasion)
                        Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.Pokemon_name+" missed the attack");
                    else
                        Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.Pokemon_name+" dodged the attack");
                }
                else
                    return true;
            }else
                return true;
        }
        else
        {
            if (attacker.pokemon.isFlinched)
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.Pokemon_name+" flinched!");
            else if(attacker.pokemon.Status_effect!="None")
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.Pokemon_name+" is affected by "+ attacker.pokemon.Status_effect);
        }
        return false;
    }
    private IEnumerator ExecuteMoves(List<Turn> turnOrder)
    {
        foreach (Turn currentTurn in turnOrder )
        {
            var attacker=Battle_handler.Instance.battleParticipants[currentTurn.attackerIndex];
            var victim=Battle_handler.Instance.battleParticipants[currentTurn.victimIndex];
            if (!IsValidParticipantState(attacker))
                continue;
            if (!IsValidParticipant(currentTurn,attacker))
                continue;
            if (!IsValidParticipantState(victim))
            {//if attack was directed at a pokemon that just fainted
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.Pokemon_name+" missed the attack");
                yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
                continue;
            }
            OnMoveExecute?.Invoke();
            yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
            if (CanAttack(currentTurn,attacker,victim))
            {
                yield return new WaitUntil(() => !levelEventDelay);
                yield return new WaitUntil(() => !faintEventDelay);
                Move_handler.Instance.doingMove = true;
                CheckRepeatedMove(attacker,currentTurn.move);
                Move_handler.Instance.ExecuteMove(currentTurn);
                yield return new WaitUntil(() => !Move_handler.Instance.doingMove);
                yield return new WaitUntil(() => !levelEventDelay);
                yield return new WaitUntil(() => !faintEventDelay);
            }
            else
                yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        yield return new WaitUntil(() => !levelEventDelay);
        yield return new WaitUntil(() => !faintEventDelay);
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
        _turnHistory.Clear();
        OnTurnEnd?.Invoke();
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
        Battle_handler.Instance.ResetMoveUsability();
        NextTurn();
    }

    void CheckRepeatedMove(Battle_Participant attacker, Move move)
    {
        if (string.IsNullOrEmpty(attacker.previousMove))
        {
            attacker.previousMove = move.Move_name + "/0";
            return;
        }
        var previousMoveName = attacker.previousMove.Split('/')[0];
        var previousMoveRepetitions = int.Parse(attacker.previousMove.Split('/')[1]);
        
        attacker.previousMove = (previousMoveName == move.Move_name)?
             move.Move_name +"/"+ (previousMoveRepetitions + 1) : move.Move_name + "/0";
    }
    bool IsValidParticipantState(Battle_Participant participant)
    {
        if (!participant.isActive) return false;
        if (participant.fainted) return false;
        return participant.pokemon != null;
    }

    bool IsValidParticipant(Turn turn,Battle_Participant participant)
    {
        return (turn.attackerID == participant.pokemon.Pokemon_ID.ToString() |
                turn.victimID == participant.pokemon.Pokemon_ID.ToString());
    }
    public void NextTurn()
    { 
        if ( Battle_handler.Instance.isDoubleBattle)
            ChangeTurn(4,1);
        else
            ChangeTurn(3,2);
    }
    public void ChangeTurn(int participantIndex,int step)
    {
        if (currentTurnIndex < participantIndex-1)
            currentTurnIndex+=step;
        else
        {
            Battle_handler.Instance.ViewOptions();
            currentTurnIndex = 0;
            Battle_handler.Instance.battleParticipants[currentTurnIndex].enemySelected = false;//allow player to attack
        }
        OnNewTurn?.Invoke();
        Battle_handler.Instance.doingMove = false;
        if (!Battle_handler.Instance.battleParticipants[currentTurnIndex].isActive & Options_manager.Instance.playerInBattle)
            NextTurn();
    }
    private bool MoveSuccessfull(Turn turn)
    {
        var rand = Utility.RandomRange(1, 100);
        var hitChance = turn.move.Move_accuracy *
                           (Battle_handler.Instance.battleParticipants[turn.attackerIndex].pokemon.Accuracy / 
                            Battle_handler.Instance.battleParticipants[turn.victimIndex].pokemon.Evasion);
        return hitChance>rand;
    }
    private List<Turn> Set_priority()
    {
        var orderBySpeed = _turnHistory.OrderByDescending(p => Battle_handler.Instance.battleParticipants[p.attackerIndex].pokemon.speed).ToList();
        var priorityList = orderBySpeed.OrderByDescending(p => p.move.Priority).ToList();
        return priorityList;
    }
}
