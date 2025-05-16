using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;
using UnityEngine.Serialization;

public class DialogueOptionsManager : MonoBehaviour
{
    public List<DialogueOption> currentOptions;
    private int _widthMultiplier = 16;
    private int _heightMultiplier = 50;
    private int _minWidth = 85;
    RectTransform rectTransform;
    public void LoadUiSize()
    {
        rectTransform = GetComponent<RectTransform>();
        var longestOptionLength = currentOptions.OrderByDescending(option => option.textContent).First().textContent.Length;
        var width = longestOptionLength * _widthMultiplier;
        if (width < _minWidth) width = _minWidth;
        var height =  currentOptions.Count*_heightMultiplier;
        rectTransform.localScale = new Vector3(width,height, 0);
    }
}
