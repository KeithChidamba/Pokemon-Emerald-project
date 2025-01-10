using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Trainer Data", menuName = "trainer data")]
public class TrainerData : ScriptableObject
{
    public string TrainerName = "";
    public string TrainerType = "";
    public Encounter_Area TrainerLocation;
    public List<TrainerPokemonData> PokemonMovesets = new();
}
