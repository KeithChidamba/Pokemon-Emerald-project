using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Recieve_Pokemon : MonoBehaviour
{
    [FormerlySerializedAs("pkm")] public Overworld_interactable[] giftPokemon;
   public void PickGiftPokemon(string giftPokemonName)
   {
        foreach (var giftInteraction in giftPokemon)
        {
            if (giftInteraction.interaction.ResultMessage == giftPokemonName)
                giftInteraction.gameObject.SetActive(false);
            giftInteraction.gameObject.layer = 0;//prevent them from being interacted with
        }
    }
}
