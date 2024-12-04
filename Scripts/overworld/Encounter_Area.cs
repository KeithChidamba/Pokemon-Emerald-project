using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Encounter", menuName = "enc")]
public class Encounter_Area : ScriptableObject
{
    public string Biome_name;
    public string[] Pokemon = {"","","",""}; //4 pokemon per area, contains pokemon name and encounter chance 007%, from smallest to greatest chance
    public int min_lv = 0;
    public int max_lv = 0;
}
