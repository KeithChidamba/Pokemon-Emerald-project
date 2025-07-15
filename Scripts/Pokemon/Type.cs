using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Pkm_types", menuName = "p_types")]
public class Type : ScriptableObject 
{
    public string typeName;
    public PokemonOperations.Types[] weaknesses;
    public PokemonOperations.Types[] resistances; 
    public PokemonOperations.Types[] immunities;
    public Sprite typeImage;
}
