using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Pkm_types", menuName = "p_types")]
public class Type : ScriptableObject 
{
    public string Type_name;
    public string[] weaknesses;
    public string[] Resistances;
    public string[] Non_effect;
    public Sprite type_img;
}
