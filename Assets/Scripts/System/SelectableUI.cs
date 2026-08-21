using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class SelectableUI
{
    public GameObject uiObject;
    public Action eventForUi;
    public bool canBeSelected;
    public SelectableUI(GameObject ui, Action eventForUi,  bool canBeSelected)
    {
        uiObject = ui;
        this.eventForUi = eventForUi;
        this.canBeSelected = canBeSelected;
    }
}
