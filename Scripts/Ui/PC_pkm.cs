using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PC_pkm : MonoBehaviour
{
    public Pokemon pokemon;
    public Image pokemonImage;
    public bool isEmpty;
    public void SetImage()
    {
        pokemonImage = GetComponent<Image>();
    }

    public void LoadImage()
    {
        pokemonImage.sprite = pokemon.partyFrame1;
    }
}
