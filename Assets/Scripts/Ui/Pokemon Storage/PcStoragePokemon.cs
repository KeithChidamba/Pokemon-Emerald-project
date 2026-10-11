using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PcStoragePokemon : MonoBehaviour
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
        pokemonImage.color = Color.white;
        pokemonImage.sprite = pokemon.partyFrame1;
    }
}
