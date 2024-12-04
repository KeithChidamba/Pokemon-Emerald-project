using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Interable_obj", menuName = "obj")]
public class Interaction : ScriptableObject
{
    public string InteractionMsg = "";
    public string InterAction_type = "";    //options,details,info,List of many options
    public List<string> InterAction_options = new();
    public string InterAction_result_msg = "";
    public List<string> options_txt= new();
}
