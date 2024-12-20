using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Evolution", menuName = "pkm_evo")]
public class Evolution : ScriptableObject
{
    public string Evo_name;
    public List<Type> types;
    public Ability ability;
    public int exp_yield=0;
    public string[] learnSet;
    public Sprite front_picture;
    public Sprite back_picture;
    public float BaseHP;
    public float BaseAttack;
    public float BaseDefense;
    public float BaseSP_ATK;
    public float BaseSP_DEF;
    public float Basespeed;
}
