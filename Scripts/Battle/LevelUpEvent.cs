using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelUpEvent : ICommand
{
    public Pokemon pokemon;
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
