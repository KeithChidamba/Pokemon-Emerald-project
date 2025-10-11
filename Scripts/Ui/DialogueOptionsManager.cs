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
    [SerializeField]private int widthMultiplier = 19;
    [SerializeField]private int heightMultiplier = 50;
    [SerializeField]private int minWidth = 150;
    [SerializeField] private float selectorPositionMultiplier = -0.8f;
    private RectTransform _rectTransform;
    private int _selectorWidth = 40;
    private void OnEnable()
    {
        _rectTransform = GetComponent<RectTransform>();
    }
    public void LoadUiSize()
    {
        var listOfLongest = currentOptions.OrderByDescending(option => option.textContent.Length).ToList();
        var longestOptionLength = listOfLongest.First().textContent.Length;
        var width = longestOptionLength * widthMultiplier;
        width += _selectorWidth;
        if (width < minWidth) width = minWidth;
        var height =  currentOptions.Count*heightMultiplier;
        foreach (var option in currentOptions)
            option.SetWidth(width);
      
        var selectorImage = Dialogue_handler.Instance.optionSelector.transform.GetChild(0);
        var selectorRect = selectorImage.GetComponentInChildren<RectTransform>();
        var yPos = selectorRect.anchoredPosition.y;
        _rectTransform.sizeDelta = new Vector2(width, height - currentOptions.Count);
        
        selectorRect.anchoredPosition = new Vector2(selectorPositionMultiplier*width,yPos);
    }
}
