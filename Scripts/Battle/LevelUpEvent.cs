using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class LevelUpEvent
{
    [SerializeField]public Pokemon pokemon;
    public LevelUpEvent(Pokemon pokemon)
    {
        this.pokemon = pokemon;
    }
    public void Execute()
    {
        PokemonOperations.GetNewMove(pokemon);
    }
}
