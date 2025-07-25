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
    [SerializeField]List<Turn> _turnHistory = new();
    public event Action OnNewTurn;
    public event Action<Battle_Participant> OnMoveExecute;
    public event Action OnTurnsCompleted;
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
        OnNewTurn += AllowPlayerInput;
    }

    public void SaveMove(Turn turn)
    {
        _turnHistory.Add(turn);
        if ((Battle_handler.Instance.isDoubleBattle && IsLastParticipant())
            || (currentTurnIndex == Battle_handler.Instance.participantCount))
        {
            InputStateHandler.Instance.AddPlaceHolderState();
            StartCoroutine(ExecuteMoves(SetPriority()));    
        }
        else
        {
            InputStateHandler.Instance.ResetRelevantUi(new[]{InputStateHandler.StateName.PokemonBattleMoveSelection
                ,InputStateHandler.StateName.PokemonBattleEnemySelection});
            NextTurn();
        }
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
        currentTurnIndex = 0;
        _turnHistory.Clear();
        faintEventDelay = false;
        levelEventDelay = false;
        StopAllCoroutines();
    }
    private bool CanAttack(Turn turn, Battle_Participant attacker,Battle_Participant victim)
    {
        if(attacker.pokemon.hp<=0) return false;
        if (attacker.canAttack)
        {
            if (attacker.isConfused)
            {
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName + " is confused");
                if (Utility.RandomRange(0, 2) < 1)
                {
                    Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" hurt itself in its confusion");
                    Move_handler.Instance.ConfusionDamage(attacker);
                    return false;
                }
            }
            if (turn.move.moveAccuracy < 100)//not a sure-hit move
            {
                if (!MoveSuccessful(turn))
                {
                    if(attacker.pokemon.accuracy >= victim.pokemon.evasion)
                        Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" missed the attack");
                    else
                        Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.pokemonName+" dodged the attack");
                }
                else
                    return true;
            }else
                return true;
        }
        else
        {
            if (attacker.isFlinched)
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" flinched!");
            else if(attacker.pokemon.statusEffect!=PokemonOperations.StatusEffect.None)
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" is affected by "+ attacker.pokemon.statusEffect);
        }
        return false;
    }
    private IEnumerator ExecuteMoves(List<Turn> turnOrder)
    {
        foreach (var currentTurn in turnOrder )
        {
            if (Battle_handler.Instance.battleOver) break;

            var attacker=Battle_handler.Instance.battleParticipants[currentTurn.attackerIndex];
            var victim=Battle_handler.Instance.battleParticipants[currentTurn.victimIndex];
            if (!IsValidParticipantState(attacker))
                continue;
            
            if (!IsValidParticipant(currentTurn,attacker))
                continue;
            
            if (!IsValidParticipantState(victim))
            {//if attack was directed at a pokemon that just fainted
               
                Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.pokemonName+" missed the attack");
                yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
                continue;
            }
            OnMoveExecute?.Invoke(attacker);
            yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
            if (CanAttack(currentTurn,attacker,victim))//test if confusion damage is waited for
            {
                yield return new WaitUntil(() => !Item_handler.Instance.usingHeldItem);
                yield return new WaitUntil(() => !levelEventDelay);
                yield return new WaitUntil(() => Battle_handler.Instance.faintQueue.Count == 0 && !faintEventDelay);
                Move_handler.Instance.doingMove = true;
                CheckRepeatedMove(attacker,currentTurn.move);
                Move_handler.Instance.ExecuteMove(currentTurn);
                
                yield return new WaitUntil(() => !Move_handler.Instance.doingMove);
                yield return new WaitUntil(() => !Item_handler.Instance.usingHeldItem);
                yield return new WaitUntil(() => !levelEventDelay);
                yield return new WaitUntil(() => Battle_handler.Instance.faintQueue.Count == 0 && !faintEventDelay);
            }
            else
                yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        yield return new WaitUntil(() => !levelEventDelay);
        yield return new WaitUntil(() => Battle_handler.Instance.faintQueue.Count == 0 && !faintEventDelay);
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
        _turnHistory.Clear();
        OnTurnsCompleted?.Invoke();
        yield return new WaitUntil(()=> !Dialogue_handler.Instance.messagesLoading);
        NextTurn();
    }

    void AllowPlayerInput()
    {
        if (currentTurnIndex > 1) return;
        InputStateHandler.Instance.ResetRelevantUi(new[]{ InputStateHandler.StateName.DialoguePlaceHolder});
        InputStateHandler.Instance.ResetRelevantUi(new[]{InputStateHandler.StateName.PokemonBattleEnemySelection,
            InputStateHandler.StateName.PlaceHolder});
    }
    private void CheckRepeatedMove(Battle_Participant attacker, Move move)
    {
        if (attacker.previousMove==null)
        {
            var newData = new PreviousMove(move,0);
            attacker.previousMove = newData;
            return;
        }
        if (attacker.previousMove.move.moveName == move.moveName)
            attacker.previousMove.numRepetitions++;
        else
        {
            var newData = new PreviousMove(move,0);
            attacker.previousMove = newData;
        }
    }
    private bool IsValidParticipantState(Battle_Participant participant)
    {
        if (!participant.isActive) return false;
        if (participant.fainted) return false;
        return participant.pokemon != null;
    }

    private bool IsValidParticipant(Turn turn,Battle_Participant participant)
    {
        return turn.attackerID == participant.pokemon.pokemonID||
                turn.victimID == participant.pokemon.pokemonID;
    }
    public void NextTurn()
    { 
        if ( Battle_handler.Instance.isDoubleBattle)
            ChangeTurn(3,1);
        else
            ChangeTurn(2,2);
    }

    public void RemoveTurn()
    {//player wants to change their turn usage
        if (currentTurnIndex < 1) return;
        _turnHistory.RemoveAt(currentTurnIndex-1);
        currentTurnIndex --;
        InputStateHandler.Instance.OnStateRemovalComplete += Battle_handler.Instance.SetupOptionsInput;
    }
    public void ChangeTurn(int maxParticipantIndex,int step)
    {
        if (currentTurnIndex < maxParticipantIndex)
            currentTurnIndex+=step;
        else
            currentTurnIndex = 0;

        OnNewTurn?.Invoke();
        
        if (!Battle_handler.Instance.battleParticipants[currentTurnIndex].isActive & Options_manager.Instance.playerInBattle)
            NextTurn();
        
    }
    public bool MoveSuccessful(Turn turn)
    {
        var random = Utility.RandomRange(1, 100);
        var hitChance = turn.move.moveAccuracy *
                           (Battle_handler.Instance.battleParticipants[turn.attackerIndex].pokemon.accuracy / 
                            Battle_handler.Instance.battleParticipants[turn.victimIndex].pokemon.evasion);
        return hitChance>random;
    }
    private List<Turn> SetPriority()
    {
        var orderBySpeed = _turnHistory.OrderByDescending(p => Battle_handler.Instance.battleParticipants[p.attackerIndex].pokemon.speed).ToList();
        var priorityList = orderBySpeed.OrderByDescending(p => p.move.priority).ToList();
        return priorityList;
    }
}
