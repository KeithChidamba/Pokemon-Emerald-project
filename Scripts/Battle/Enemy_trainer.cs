using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Enemy_trainer : MonoBehaviour
{
    public Battle_Participant participant;
    [FormerlySerializedAs("_trainerData")] public TrainerData trainerData;
    [FormerlySerializedAs("TrainerParty")] public List<Pokemon> trainerParty;
    [FormerlySerializedAs("InBattle")] public bool inBattle = false;
    [FormerlySerializedAs("CanAttack")] public bool canAttack = true;
    [FormerlySerializedAs("Used_move")] [SerializeField]private bool usedMove = false;
    private void Update()
    {
        if (!inBattle) return;
        Make_Decision();
    }
    private void Start()
    {
        Turn_Based_Combat.instance.OnNewTurn += Reset_move;
        Battle_handler.Instance.OnBattleEnd += ResetAfterBattle;
    }
    public void Can_Attack()
    {
        canAttack = true;
    }

    void ResetAfterBattle()
    {
        trainerData = null;
        trainerParty.Clear();
    }
    void Reset_move()
    {
        if (!inBattle) return;
        usedMove = false;
    }

    public void CheckLoss()
    {
        List<Pokemon> numAlive = trainerParty.ToList();
        numAlive.RemoveAll(p => p.HP <= 0);
        if (numAlive.Count == 0)
        {
            Battle_handler.Instance.lastOpponent = participant.pokemon;
            Battle_handler.Instance.End_Battle(true);
        }
        else
        {
            if (Battle_handler.Instance.isDoubleBattle)//double battle
            {//only select the pokemon that werent in battle
                List<Pokemon> NotParticipatingList = new();
                foreach (Pokemon pokemon in trainerParty)
                    if(pokemon!=Battle_handler.Instance.battleParticipants[2].pokemon && pokemon!=Battle_handler.Instance.battleParticipants[3].pokemon)
                        NotParticipatingList.Add(pokemon);
                NotParticipatingList.RemoveAll(p => p.HP <= 0);
                if (NotParticipatingList.Count == 0)
                {//1 left
                    if(participant.pokemon.HP<=0)
                    {
                        participant.Deactivate_pkm();
                        participant.pokemon = null;
                        participant.isActive = false;
                        inBattle = false;
                        participant.DeactivateUI();
                        Battle_handler.Instance.CountParticipants();
                    }
                }
                else
                {
                    int randomLeftOver = Utility.RandomRange(0, NotParticipatingList.Count - 1);
                    participant.pokemon = NotParticipatingList[randomLeftOver];
                    Battle_handler.Instance.SetParticipant(participant);
                }
            }
            else
            {
                int randomMemeber = Utility.RandomRange(0, numAlive.Count - 1);
                participant.pokemon = numAlive[randomMemeber];
                Battle_handler.Instance.SetParticipant(participant);
            }
        }
        Turn_Based_Combat.instance.FaintEventDelay = false;
    }
    public void StartBattle(string TrainerName, bool isSameTrainer)
    {
        participant = GetComponent<Battle_Participant>();
        if (isSameTrainer) return;
        TrainerData copy = Resources.Load<TrainerData>("Pokemon_project_assets/Enemies/Data/" + TrainerName +"/"+ TrainerName);
        trainerData = Obj_Instance.SetTrainer(copy);
        foreach (TrainerPokemonData member in trainerData.PokemonParty)
        {
            trainerParty.Add(member.pokemon);
            int exp = PokemonOperations.GetNextLv(member.PokemonLevel, member.pokemon.EXPGroup)+1;
            member.pokemon.Recieve_exp(exp);
            member.pokemon.HP = member.pokemon.max_HP;
            member.pokemon.move_set.Clear();
            foreach (Move move in member.moveSet)
                member.pokemon.move_set.Add(Obj_Instance.set_move(move));
            if (member.hasItem)
                member.pokemon.HeldItem = Obj_Instance.set_Item(member.heldItem);
        }
    }
    void use_move(Move move)
    {
        //Debug.Log(Turn_Based_Combat.instance.Current_pkm_turn+" "+Used_move+" move: "+move.Move_name);
        Battle_handler.Instance.UseMove(move,participant);
        usedMove = true;
    }
    public void Select_player(int selectedIndex)
    {
        //enemy choosing player
        participant.enemySelected = true;
        Battle_handler.Instance.currentEnemyIndex = selectedIndex;
    }
    private void Make_Decision()
    {
            if (Battle_handler.Instance.battleParticipants[Turn_Based_Combat.instance.Current_pkm_turn].pokemon == participant.pokemon && !usedMove && canAttack)
            {
                //Debug.Log("ai ataccked wit: "+participant.pokemon.Pokemon_name);
                int randome_enemy = Utility.RandomRange(0, participant.currentEnemies.Count);
                Select_player(randome_enemy);
                int randomMove = Utility.RandomRange(0, participant.pokemon.move_set.Count);
                //Debug.Log(participant.pokemon.Pokemon_name+" is gonna use move: "+participant.pokemon.move_set[randomMove].Move_name);
                use_move(participant.pokemon.move_set[randomMove]);
                canAttack = false;
            }
    }
}
