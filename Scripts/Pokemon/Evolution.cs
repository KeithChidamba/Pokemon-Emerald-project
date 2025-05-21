using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Evolution", menuName = "pkm_evo")]
public class Evolution : ScriptableObject
{
    [FormerlySerializedAs("Evo_name")] public string evolutionName;
    public List<Type> types;
    public Ability ability;
    [FormerlySerializedAs("EXPGroup")] public string expGroup = "";
    [FormerlySerializedAs("exp_yield")] public int expYield=0;
    [FormerlySerializedAs("CatchRate")] public float catchRate = 0;
    public string[] learnSet;
    [FormerlySerializedAs("front_picture")] public Sprite frontPicture;
    [FormerlySerializedAs("back_picture")] public Sprite backPicture;
    [FormerlySerializedAs("BaseHP")] public float baseHp;
    [FormerlySerializedAs("BaseAttack")] public float baseAttack;
    [FormerlySerializedAs("BaseDefense")] public float baseDefense;
    [FormerlySerializedAs("BaseSP_ATK")] public float baseSpecialAttack;
    [FormerlySerializedAs("BaseSP_DEF")] public float baseSpecialDefense;
    [FormerlySerializedAs("Basespeed")] public float baseSpeed;
    [FormerlySerializedAs("EVs")] public List<string> effortValues=new();
}
