using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DialogueOption : MonoBehaviour
{
    public Text optionText;
    public int optionIndex;
    public string textContent;
    private const int VerticalOffset = 35;

    private RectTransform _rectTransform;
    public void SetupOption(int index,int numOptions, string text)
    {
        _rectTransform = GetComponent<RectTransform>();
        optionIndex = index;
        optionText = GetComponentInChildren<Text>();
        textContent = text;
        optionText.text = textContent;
        //pad out height when there's a lot of options
        var heightPadding = numOptions>5? 5:numOptions;
        //stack options in column
        _rectTransform.localPosition = new Vector3(_rectTransform.localPosition.x
            , _rectTransform.localPosition.y+( (numOptions-1-optionIndex) * VerticalOffset) + (heightPadding*5), 0);
    }

    public void SetWidth(int width)
    {
        var shrinkFactor = 0.8f;//makes the options ui a little smaller than it's set size
        _rectTransform.sizeDelta = new Vector2( width*shrinkFactor, _rectTransform.rect.height);
    }
}
