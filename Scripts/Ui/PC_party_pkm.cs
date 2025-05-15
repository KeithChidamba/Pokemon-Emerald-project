using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PC_party_pkm : MonoBehaviour
{
    public int partyPosition;
    public Image pokemonSprite;
    public Pokemon pokemon;
    public GameObject options;

    public void LoadImage()
    {
        pokemonSprite = GetComponent<Image>();
        pokemonSprite.sprite = pokemon.front_picture;
    }
}
