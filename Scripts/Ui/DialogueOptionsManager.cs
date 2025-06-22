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
    private int _widthMultiplier = 10;
    private int _heightMultiplier = 32;
    private int _minWidth = 100;
    RectTransform rectTransform;

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    public void LoadUiSize()
    {
        var listOfLongest = currentOptions.OrderByDescending(option => option.textContent.Length).ToList();
        var longestOptionLength = listOfLongest.First().textContent.Length;
        var width = longestOptionLength * _widthMultiplier;
        if (width < _minWidth) width = _minWidth;
        var height =  currentOptions.Count*_heightMultiplier;
        foreach (var option in currentOptions)
            option.SetWidth(width);
      
        var selectorImage = Dialogue_handler.Instance.optionSelector.transform.GetChild(0);
        var padding = width * 0.2f > 25 ? 25 : width * 0.2f;
        selectorImage.GetComponentInChildren<RectTransform>().sizeDelta = new Vector2(width+padding, 27);
        rectTransform.sizeDelta = new Vector2(width, height - currentOptions.Count);
        
    }
}
