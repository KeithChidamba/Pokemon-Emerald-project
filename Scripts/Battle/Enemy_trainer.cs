using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_trainer : MonoBehaviour
{
    public TrainerData _TrainerData;
    public void StartBattle(string TrainerName)
    {
        Debug.Log("battle start");
        _TrainerData = Obj_Instance.SetTrainer(Resources.Load<TrainerData>("Pokemon_project_assets/Enemies/Data" + TrainerName));
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
}
