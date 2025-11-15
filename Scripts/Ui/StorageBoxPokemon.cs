using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "box pokemon", menuName = "box pkm")]
public class StorageBoxPokemon : ScriptableObject
{
    public int boxNumber;
    public long pokemonID;
    public int positionInBox;
}
