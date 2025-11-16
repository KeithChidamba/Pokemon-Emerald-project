using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Storage box", menuName = "storage box")]
public class PokemonStorageBox : ScriptableObject
{
    public int boxNumber;
    public List<StorageBoxPokemon> boxPokemon;
    public int currentNumPokemon;
    public Sprite boxTopVisual;
    public Sprite boxVisual;
}
