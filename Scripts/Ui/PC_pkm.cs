using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PC_pkm : MonoBehaviour
{
    public Pokemon pokemon;
    public Image pokemonSprite;
    
    public void LoadImage()
    {
        pokemonSprite = GetComponent<Image>();
        pokemonSprite.sprite = pokemon.frontPicture;
    }
}
