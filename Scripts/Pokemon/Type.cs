using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Pkm_types", menuName = "p_types")]
public class Type : ScriptableObject 
{
    public string Type_name;
    public string[] weaknesses;
    public string[] Resistances;
    public string[] Non_effect;//immune
    public Sprite type_img;
    
    public bool type_check(string[]check ,Type type)
    {
        foreach (string c in check)
        {
            if (c == type.Type_name)
            {
                return true;
            }
        }
        return false;
    }
}
