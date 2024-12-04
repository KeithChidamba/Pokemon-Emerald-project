using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recieve_Pokemon : MonoBehaviour
{
    public Overworld_interactable[] pkm;
   public void check_pkm(string gift_pkm)
   {
        foreach (Overworld_interactable p in pkm)
        {
            if (p.interaction.InterAction_result_msg == gift_pkm)
            {
                p.gameObject.SetActive(false);
            }
            p.gameObject.layer = 0;//prevent them from being interacted with
        }
    }
}
