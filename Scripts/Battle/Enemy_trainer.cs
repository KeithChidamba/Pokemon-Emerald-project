using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_trainer : MonoBehaviour
{
    public Battle_Participant participant;
    public TrainerData _TrainerData;
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
    public void StartBattle(string TrainerName)
    {
        participant = GetComponent<Battle_Participant>();
        TrainerData copy = Resources.Load<TrainerData>("Pokemon_project_assets/Enemies/Data/" + TrainerName +"/"+ TrainerName);
        _TrainerData = Obj_Instance.SetTrainer(copy);
        foreach (TrainerPokemonData member in _TrainerData.PokemonMovesets)
        {
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
        Battle_handler.instance.Use_Move(move,participant);
        Debug.Log("here "+participant.Current_Enemies.Count+" move: "+move.Move_name);
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
            Select_player(Utility.Get_rand(0,participant.Current_Enemies.Count));
            use_move(participant.pokemon.move_set[Utility.Get_rand(0,participant.pokemon.move_set.Count)]);//random move

        }
    }
}
