using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Pkm_types", menuName = "Pokemon/Type")]
public class Type : ScriptableObject 
{
    public string typeName;
    public Types[] weaknesses;
    public Types[] resistances; 
    public Types[] immunities;
    public Sprite typeImage;
}
