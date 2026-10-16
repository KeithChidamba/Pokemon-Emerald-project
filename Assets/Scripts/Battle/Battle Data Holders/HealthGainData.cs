using System;
[Serializable]
public class HealthGainData
{
    public PokemonStateHandler pokemonState;
    public float healthChange;
    public HealthGainData(float healthChange, PokemonStateHandler pokemonState)
    {
        this.healthChange = healthChange;
        this.pokemonState = pokemonState;
    }
}

