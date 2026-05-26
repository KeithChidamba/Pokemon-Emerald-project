using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogueOptionsManager : MonoBehaviour,IInjectable
{
    public List<DialogueOption> currentOptions;
    [SerializeField]private int widthMultiplier = 19;
    [SerializeField]private int heightMultiplier = 50;
    [SerializeField]private int minWidth = 100;
    [SerializeField] private float selectorPositionMultiplier = -0.9f;
    private RectTransform _rectTransform;
    private int _selectorWidth = 40;
    
    private Dialogue_handler _dialogueHandler;
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
    }
    
    public void OnInject()
    {
        
    }
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
      
        var selectorImage = _dialogueHandler.optionSelector.transform.GetChild(0);
        var selectorRect = selectorImage.GetComponentInChildren<RectTransform>();
        var yPos = selectorRect.anchoredPosition.y;
        _rectTransform.sizeDelta = new Vector2(width + _selectorWidth*0.5f, height - currentOptions.Count);
        
        selectorRect.anchoredPosition = new Vector2(selectorPositionMultiplier*width,yPos);
    }
}
