using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class LevelUpEvent : ICommand
{
    [SerializeField]public Pokemon pokemon;
    public LevelUpEvent(Pokemon pokemon_)
    {
        pokemon = pokemon_;
    }
    public void Execute()
    {
        PokemonOperations.GetNewMove(pokemon);
    }
    public void Undo()
    {

    }
}
