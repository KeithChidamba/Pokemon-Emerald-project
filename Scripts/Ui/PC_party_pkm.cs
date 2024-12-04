using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PC_party_pkm : MonoBehaviour
{
    public int party_pos;
    public Image pkm_img;
    public Pokemon pkm;
    public GameObject options;
    public void Load_image()
    {
        pkm_img.sprite = pkm.front_picture;
    }
}
