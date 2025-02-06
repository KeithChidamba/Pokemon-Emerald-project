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
        Battle_handler.instance.onBattleEnd += ResetAfterBattle;
    }
    public void Can_Attack()
    {
        CanAttack = true;
    }

    void ResetAfterBattle()
    {
        _TrainerData = null;
        TrainerParty.Clear();
    }
    void Reset_move()
    {
        if (!InBattle) return;
        Used_move = false;
    }

    public void CheckLoss()
    {
        List<Pokemon> numAlive = TrainerParty.ToList();
        numAlive.RemoveAll(p => p.HP <= 0);
        //Debug.Log("alive: "+numAlive.Count);
        if (numAlive.Count == 0)
        {
            Battle_handler.instance.LastOpponent = participant.pokemon;
            Battle_handler.instance.End_Battle(true);
        }
        else
        {
            if (Battle_handler.instance.isDouble_battle)//double battle
            {//only select the pokemon that werent in battle
                List<Pokemon> NotParticipatingList = new();
                foreach (Pokemon pokemon in TrainerParty)
                    if(pokemon!=Battle_handler.instance.Battle_Participants[2].pokemon && pokemon!=Battle_handler.instance.Battle_Participants[3].pokemon)
                        NotParticipatingList.Add(pokemon);
                // = TrainerParty.GetRange(2, TrainerParty.Count-2);
                NotParticipatingList.RemoveAll(p => p.HP <= 0);
                //Debug.Log("partic alive: "+NotParticipatingList.Count);
                if (NotParticipatingList.Count == 0)
                {//1 left
                    if(participant.pokemon.HP<=0)
                    {
                       // Debug.Log("ui reset");
                        participant.Deactivate_pkm();
                        participant.pokemon = null;
                        participant.is_active = false;
                        InBattle = false;
                        participant.Unload_ui();
                        Battle_handler.instance.check_Participants();
                    }
                }
                else
                {
                    int randomLeftOver = Utility.Get_rand(0, NotParticipatingList.Count - 1);
                    participant.pokemon = NotParticipatingList[randomLeftOver];
                    Battle_handler.instance.Set_participants(participant);
                }
            }
            else
            {
                int randomMemeber = Utility.Get_rand(0, numAlive.Count - 1);
                participant.pokemon = numAlive[randomMemeber];
                Battle_handler.instance.Set_participants(participant);
            }
            Turn_Based_Combat.instance.FainEventDelay = false;
        }
    }
    public void StartBattle(string TrainerName, bool isSameTrainer)
    {
        participant = GetComponent<Battle_Participant>();
        if (isSameTrainer) return;
        TrainerData copy = Resources.Load<TrainerData>("Pokemon_project_assets/Enemies/Data/" + TrainerName +"/"+ TrainerName);
        _TrainerData = Obj_Instance.SetTrainer(copy);
        foreach (TrainerPokemonData member in _TrainerData.PokemonParty)
        {
            TrainerParty.Add(member.pokemon);
            for(int i=0;i<member.PokemonLevel;i++)
                member.pokemon.Level_up();
            member.pokemon.HP = member.pokemon.max_HP;
            //for testing
            /*member.pokemon.HP = 1;
            member.pokemon.Attack = 1;
            member.pokemon.SP_ATK = 1;
            member.pokemon.speed = 1;*/
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
            if (Battle_handler.instance.Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].pokemon == participant.pokemon && !Used_move && CanAttack)
            {
                Debug.Log("ai ataccked wit: "+participant.pokemon.Pokemon_name);
                int randome_enemy = Utility.Get_rand(0, participant.Current_Enemies.Count);
                Select_player(randome_enemy);
                int randomMove = Utility.Get_rand(0, participant.pokemon.move_set.Count);
                //Debug.Log(participant.pokemon.Pokemon_name+" is gonna use move: "+participant.pokemon.move_set[randomMove].Move_name);
                use_move(participant.pokemon.move_set[randomMove]);
                CanAttack = false;
            }
    }
   // }
}
