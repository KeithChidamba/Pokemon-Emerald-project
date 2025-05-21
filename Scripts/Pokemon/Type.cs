using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Pkm_types", menuName = "p_types")]
public class Type : ScriptableObject 
{
    [FormerlySerializedAs("Type_name")] public string typeName;
    public string[] weaknesses;
    [FormerlySerializedAs("Resistances")] public string[] resistances; 
    [FormerlySerializedAs("Non_effect")] public string[] unaffectedTypes;
    [FormerlySerializedAs("type_img")] public Sprite typeImage;
}
