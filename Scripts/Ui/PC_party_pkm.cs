using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PC_party_pkm : MonoBehaviour
{
    public int partyPosition;
    public Image pokemonSprite;
    public Image pokemonSpriteBg;
    public Pokemon pokemon;

    public void LoadImage()
    {
        pokemonSprite.sprite = pokemon.partyFrame2;
        pokemonSpriteBg.gameObject.SetActive(true);
    }
}
