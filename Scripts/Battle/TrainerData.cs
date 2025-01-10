using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Trainer Data", menuName = "trainer data")]
public class TrainerData : ScriptableObject
{
    public string TarinerName = "";
    public string TrainerType = "";
    public List<TrainerPokemonData> PokemonMovesets = new();
}
