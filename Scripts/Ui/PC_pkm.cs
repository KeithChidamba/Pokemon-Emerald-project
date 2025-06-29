using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PC_pkm : MonoBehaviour
{
    public Pokemon pokemon;
    public Image pokemonImage;
    
    public void LoadImage()
    {
        pokemonImage = GetComponent<Image>();
        pokemonImage.sprite = pokemon.frontPicture;
    }
}
