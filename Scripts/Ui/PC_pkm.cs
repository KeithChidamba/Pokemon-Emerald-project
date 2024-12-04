using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PC_pkm : MonoBehaviour
{
    public Pokemon pkm;
    public Image pkm_sprite;
    public GameObject options;
    public void Load_image()
    {
        pkm_sprite.sprite = pkm.front_picture;
    }
}
