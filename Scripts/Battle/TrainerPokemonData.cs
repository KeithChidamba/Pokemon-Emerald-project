using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Trainer Pkm Data", menuName = "Battle/Trainer/trainer pkm data")]
public class TrainerPokemonData : ScriptableObject
{
    public Pokemon pokemon;
    public int pokemonLevel;
    public int evolutionStageNumber;
    public bool hasItem;
    public Item heldItem;
    public List<Move> moveSet=new();
}
