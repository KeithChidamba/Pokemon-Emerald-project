using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pokemon_party_member : MonoBehaviour
{
    public Text Pkm_name;
    public Text Pkm_Lv;
    public Image Pkm_front_img,Status_img;
    public Slider pkm_hp;
    public Pokemon pkm;
    public int Party_pos = 0;
    public GameObject Options;
    public GameObject[] main_ui;
    public GameObject empty_ui;
    public GameObject HeldItem_img;
    public bool isEmpty = false;
    private void Start()
    {
        Options.SetActive(false);
    }

    public void Levelup()
    {
        //debugging purposes
        pkm.Level_up();
    }
    public void Set_Ui()
    {
        Pkm_front_img.sprite = pkm.front_picture;
        foreach (GameObject ui in main_ui)
            ui.SetActive(true);
        isEmpty = false;
        empty_ui.SetActive(false);
        if (pkm.HasItem)
            HeldItem_img.SetActive(true);
        else
            HeldItem_img.SetActive(false);
        if (pkm.Status_effect == "None")
            Status_img.gameObject.SetActive(false);
        else
        {
            Status_img.gameObject.SetActive(true);
            Status_img.sprite = Resources.Load<Sprite>("Pokemon_project_assets/Pokemon_obj/Status/" + pkm.Status_effect.ToLower());
        }
    }
    public void Reset_ui()
    {
        foreach (GameObject ui in main_ui)
            ui.SetActive(false);
        isEmpty = true;
        HeldItem_img.gameObject.SetActive(false);
        Status_img.gameObject.SetActive(false);
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
            if (pkm.HP <= 0)
                Pkm_front_img.color=Color.HSVToRGB(17,96,54);//pkm fainted
            else
                Pkm_front_img.color=Color.HSVToRGB(0,0,100);
        }
    }
}
