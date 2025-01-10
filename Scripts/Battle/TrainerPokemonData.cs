using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Trainer Pkm Data", menuName = "trainer pkm data")]
public class TrainerPokemonData : ScriptableObject
{
    public Pokemon pokemon;
    public int PokemonLevel=0;
    public bool hasItem=false;
    public Item heldItem;
    public List<Move> moveSet=new();
}
