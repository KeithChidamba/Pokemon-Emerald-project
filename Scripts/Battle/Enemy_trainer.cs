using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy_trainer : MonoBehaviour
{
    public Battle_Participant participant;
    public TrainerData _TrainerData;
    public List<Pokemon> TrainerParty;
    public bool InBattle = false;
    public bool CanAttack = true;
    [SerializeField]private bool Used_move = false;
    private void Update()
    {
        if (!InBattle) return;
        Make_Decision();
    }
    private void Start()
    {
        Turn_Based_Combat.instance.OnNewTurn += Reset_move;
    }
    public void Can_Attack()
    {
        CanAttack = true;
    }
    void Reset_move()
    {
        Used_move = false;
    }

    public void CheckLoss(int ParticipantIndex)
    {
        List<Pokemon> numAlive = TrainerParty.ToList();
        numAlive.RemoveAll(p => p.HP <= 0);
        if (numAlive.Count == 0)
            Battle_handler.instance.End_Battle(true);
        else
        {
            int randomMemeber = Utility.Get_rand(0, numAlive.Count-1);
            Battle_handler.instance.Battle_P[ParticipantIndex].pokemon = numAlive[randomMemeber];
            Battle_handler.instance.Set_participants(Battle_handler.instance.Battle_P[ParticipantIndex]);
        }
    }
    public void StartBattle(string TrainerName)
    {
        participant = GetComponent<Battle_Participant>();
        TrainerData copy = Resources.Load<TrainerData>("Pokemon_project_assets/Enemies/Data/" + TrainerName +"/"+ TrainerName);
        _TrainerData = Obj_Instance.SetTrainer(copy);
        foreach (TrainerPokemonData member in _TrainerData.PokemonParty)
        {
            TrainerParty.Add(member.pokemon);
            for(int i=0;i<member.PokemonLevel;i++)
                member.pokemon.Level_up();
            member.pokemon.HP = member.pokemon.max_HP;
            member.pokemon.move_set.Clear();
            foreach (Move move in member.moveSet)
                member.pokemon.move_set.Add(Obj_Instance.set_move(move));
            if (member.hasItem)
                member.pokemon.HeldItem = member.heldItem;
        }
    }
    void use_move(Move move)
    {
        Debug.Log(" move: "+move.Move_name);
        Battle_handler.instance.Use_Move(move,participant);
        Used_move = true;
    }
    public void Select_player(int selectedIndex)
    {
        //enemy choosing player
        participant.Selected_Enemy = true;
        Battle_handler.instance.Current_pkm_Enemy = selectedIndex;
    }
    private void Make_Decision()
    {
        if (Battle_handler.instance.Battle_P[Turn_Based_Combat.instance.Current_pkm_turn].pokemon == participant.pokemon && !Used_move && CanAttack)
        {
            int randome_enemy = Utility.Get_rand(0, participant.Current_Enemies.Count);
            Select_player(randome_enemy);
            int randomMove = Utility.Get_rand(0, participant.pokemon.move_set.Count);
            Debug.Log("decision "+randome_enemy+" move: "+randomMove);
            use_move(participant.pokemon.move_set[randomMove]);//random move
        }
    }
}
