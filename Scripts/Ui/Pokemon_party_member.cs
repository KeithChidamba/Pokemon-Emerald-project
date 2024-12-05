using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pokemon_party_member : MonoBehaviour
{
    public Text Pkm_name;
    public Text Pkm_Lv;
    public Image Pkm_front_img;
    public Slider pkm_hp;
    public Pokemon pkm;
    public int Party_pos = 0;
    public GameObject Options;
    public GameObject[] main_ui;
    public GameObject empty_ui;
    public bool isEmpty = false;
    private void Start()
    {
        Options.SetActive(false);
    }
    public void Set_Ui()
    {
        Pkm_front_img.sprite = pkm.front_picture;
        foreach (GameObject ui in main_ui)
        {
            ui.SetActive(true);
        }
        isEmpty = false;
        empty_ui.SetActive(false);
    }
    public void Reset_ui()
    {
        foreach (GameObject ui in main_ui)
        {
            ui.SetActive(false);
        }
        isEmpty = true;
        empty_ui.SetActive(true);
    }
    private void Update()
    {
        if (!isEmpty)
        {
            pkm_hp.value = pkm.HP;
            pkm_hp.maxValue = pkm.max_HP;
            pkm_hp.minValue = 0;
            Pkm_Lv.text = "Lv: " + pkm.Current_level.ToString();
            Pkm_name.text = pkm.Pokemon_name;
        }
    }
}
