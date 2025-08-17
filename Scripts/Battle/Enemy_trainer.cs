using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Enemy_trainer : MonoBehaviour
{
    public Battle_Participant participant;
    public TrainerData trainerData;
    public List<Pokemon> trainerParty;
    public bool inBattle;
    public bool canAttack = true;
    [SerializeField]private bool usedMove;
    private void Update()
    {
        if (!inBattle) return;
        MakeBattleDecision();
    }
    private void Start()
    {
        Turn_Based_Combat.Instance.OnNewTurn += ResetMoveUsage;
        Battle_handler.Instance.OnBattleEnd += ResetAfterBattle;
    }
    public void CanAttack()
    {
        canAttack = true;
    }

    void ResetAfterBattle()
    {
        trainerData = null;
        trainerParty.Clear();
        canAttack = true;
        usedMove = false;
    }
    void ResetMoveUsage()
    {
        if (!inBattle) return;
        usedMove = false;
    }
    public List<Pokemon> GetLivingPokemon()
    {
        List<Pokemon> alivePokemon = new(trainerParty);
        alivePokemon.RemoveAll(p => p.hp <= 0);
        return alivePokemon;
    }
    public void CheckIfLoss()
    {
        var numAlive = GetLivingPokemon();
        if (numAlive.Count == 0)
        {
            Battle_handler.Instance.lastOpponent = participant.pokemon;
            Battle_handler.Instance.EndBattle(true);
        }
        else
        {
            if (Battle_handler.Instance.isDoubleBattle)//double battle
            {//only select the pokemon that werent in battle
                List<Pokemon> notParticipatingList = new();
                foreach (Pokemon pokemon in trainerParty)
                    if(pokemon!=Battle_handler.Instance.battleParticipants[2].pokemon
                       && pokemon!=Battle_handler.Instance.battleParticipants[3].pokemon)//not already participating
                        notParticipatingList.Add(pokemon);
                notParticipatingList.RemoveAll(p => p.hp <= 0);
                if (notParticipatingList.Count == 0)
                {//1 left
                    if(participant.pokemon.hp<=0)
                    {
                        participant.DeactivatePokemon();
                        participant.pokemon = null;
                        participant.isActive = false;
                        inBattle = false;
                        participant.DeactivateUI();
                        Battle_handler.Instance.CheckParticipantStates();
                    }
                }
                else
                {
                    var randomLeftOver = Utility.RandomRange(0, notParticipatingList.Count - 1);
                    participant.pokemon = notParticipatingList[randomLeftOver];
                    Battle_handler.Instance.SetParticipant(participant);
                }
            }
            else
            {
                var randomMember = Utility.RandomRange(0, numAlive.Count - 1);
                participant.pokemon = numAlive[randomMember];
                Battle_handler.Instance.SetParticipant(participant);
            }
        }
        Turn_Based_Combat.Instance.faintEventDelay = false;
    }
    public void SetupTrainerForBattle(string trainerName, bool isSameTrainer)
    {
        participant = GetComponent<Battle_Participant>();
        if (isSameTrainer) return;//double battle
        var copyOfTrainerData = Resources.Load<TrainerData>("Pokemon_project_assets/Enemies/Data/" + trainerName +"/"+ trainerName);
        trainerData = Obj_Instance.CreateTrainer(copyOfTrainerData);
        foreach (TrainerPokemonData member in trainerData.PokemonParty)
        {
            trainerParty.Add(member.pokemon);
            var expForNextLevel = PokemonOperations.CalculateExpForNextLevel(member.pokemonLevel, member.pokemon.expGroup)+1;
            member.pokemon.ReceiveExperience(expForNextLevel);
            member.pokemon.hp = member.pokemon.maxHp;
            member.pokemon.moveSet.Clear();
            foreach (Move move in member.moveSet)
                member.pokemon.moveSet.Add(Obj_Instance.CreateMove(move));
            
            if (member.hasItem) member.pokemon.GiveItem(Obj_Instance.CreateItem(member.heldItem));
        }
    }
    void UseMove(Move move)
    {
        Battle_handler.Instance.UseMove(move,participant);
        usedMove = true;
    }
    private void TargetPlayer(int selectedIndex)
    {
        Battle_handler.Instance.currentEnemyIndex = selectedIndex;
    }
    private void MakeBattleDecision()
    {
        if (Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex].pokemon
            != participant.pokemon)return;
        if (usedMove || !canAttack) return;
        //Debug.Log("ai ataccked wit: "+participant.pokemon.Pokemon_name);
        var randomEnemy = Utility.RandomRange(0, participant.currentEnemies.Count);
        TargetPlayer(randomEnemy);
        var randomMoveIndex = Utility.RandomRange(0, participant.pokemon.moveSet.Count);
        //Debug.Log(participant.pokemon.Pokemon_name+" is gonna use move: "+participant.pokemon.move_set[randomMoveIndex].Move_name);
        UseMove(participant.pokemon.moveSet[randomMoveIndex]);
        canAttack = false;
    }
}
