using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokemonStorageBox : ScriptableObject
{
    private int boxNumber;
    public List<StorageBoxPokemon> boxPokemon;
    private int currentNumPokemon;
    public Sprite boxVisual;
}
