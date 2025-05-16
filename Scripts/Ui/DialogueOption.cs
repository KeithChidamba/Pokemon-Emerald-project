using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DialogueOption : MonoBehaviour
{
    
    public Text optionText;
    public int optionIndex;
    public string textContent;

    public void SetupOption(int index, string text)
    {
        optionIndex = index;
        optionText = GetComponentInChildren<Text>();
        textContent = text;
        optionText.text = textContent;
        var rectTransform = GetComponent<RectTransform>();
        rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y+(optionIndex*30), 0);
    }

    public void SelectThisOption()
    {
        Dialogue_handler.Instance.SelectOption(optionIndex);
    }

}
