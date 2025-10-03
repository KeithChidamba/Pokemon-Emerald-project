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
    private RectTransform _rectTransform;
    public void SetupOption(int index,int numOptions, string text)
    {
        _rectTransform = GetComponent<RectTransform>();
        optionIndex = index;
        optionText = GetComponentInChildren<Text>();
        textContent = text;
        optionText.text = textContent;
        var offsetY = numOptions>5? 5:numOptions;
        //stack options in column
        _rectTransform.localPosition = new Vector3(_rectTransform.localPosition.x
            , _rectTransform.localPosition.y+( (numOptions-1-optionIndex)*25)+offsetY, 0);
    }

    public void SetWidth(int width)
    {
        _rectTransform.sizeDelta = new Vector2(width*0.8f, _rectTransform.rect.height);
    }
}
