using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TypingInputInterface
{
   Uppercase = 0,Lowercase = 1,Symbols = 2
}

public class TypingInterfaceGraphicData
{
   public string displayMessage;
   public bool isAnimated;
   public List<Sprite> sprites;
   public Gender displayGender;
   public Vector2 preferredGraphicSize;
   public TypingInterfaceGraphicData(bool isAnimated, List<Sprite> sprites,string displayMessage,
      Vector2 preferredSize,Gender displayGender = Gender.None)
   {
      this.isAnimated = isAnimated;
      this.sprites = sprites;
      this.displayMessage = displayMessage;
      this.displayGender = displayGender;
      preferredGraphicSize = preferredSize;
   }
}
public struct GridData
{
   public Vector2 startPosition;
   public string[] gridValues;
   public int gridSize;
   public int colCount;

   public bool hasSpecialGaps;
   public float normalGap;
   public float midGap;
   public float bigGap;
   public float verticalGap;

   public GridData(string[] values,Vector2 startPosition, float normalGap,float verticalGap, int colCount,bool hasSpecialGaps, float midGap=0f, float bigGap=0f)
   {
      this.startPosition = startPosition;
      this.hasSpecialGaps = hasSpecialGaps;
      gridValues = values;
      gridSize = values.Length;
      this.normalGap = normalGap;
      this.midGap = midGap;
      this.bigGap = bigGap;
      this.verticalGap = verticalGap;
      this.colCount = colCount;
   }
   public GridData(GridData copy)
   {
      startPosition = copy.startPosition;
      hasSpecialGaps = copy.hasSpecialGaps;
      gridValues = copy.gridValues;
      gridSize = copy.gridSize;
      normalGap = copy.normalGap;
      midGap = copy.midGap;
      bigGap = copy.bigGap;
      verticalGap = copy.verticalGap;
      colCount = copy.colCount;
   }
}
public class TypingInterfaceHandler : MonoBehaviour,IInjectable
{
   public GameObject mainUI;
   public GameObject characterSelector;
   public GameObject optionSelector;
   [SerializeField]private GameObject[] interfaceOptions;
   [SerializeField]private LoopingUiAnimation blackArrow;
   
   [SerializeField]private GameObject characterBoxTemplate;
   [SerializeField]private Transform characterBoxParent;
   [SerializeField]private List<TypingCharacterBox> characterBoxes;
   public int MaxCharacterLength { get; private set; }
      
   [SerializeField]private Image[] interfaceImages;
   public TypingInputInterface currentInterface;
   [SerializeField]private int currentInputIndex;
   private bool _isSwappingInterface;
   public int currentCharacterIndex;
   public int currentMaxBoxElements;
   [SerializeField]private string combinedInput;
   
   private Dictionary<TypingInputInterface, GridData> _interfaceGrids = new();
   [SerializeField]private List<GameObject> characterSelectables;
   [SerializeField]private GameObject characterPositionTemplate;
   [SerializeField]private Transform characterPositionParent;
   public event Action<string> OnInputResolved;
   public event Action<int> OnCharacterCaptured;
   [SerializeField]private Vector2[] symbolGridPositions;
   [SerializeField]private Vector2[] letterGridPositions;
   
   [SerializeField]private Image interfaceGraphic;
   [SerializeField]private TMP_Text interfaceDisplayText;
   [SerializeField]private Image genderGraphic;
   
   private InputStateHandler _inputStateHandler;
   private Dialogue_handler _dialogueHandler;
   
   public void Inject(ServiceContainer container)
   {
      _inputStateHandler = container.Resolve<InputStateHandler>();
      _dialogueHandler = container.Resolve<Dialogue_handler>();
      gameObject.SetActive(true);
   }
   
   public void OnInject()
   {
      _interfaceGrids.Add(TypingInputInterface.Uppercase,   
         new GridData(
            new[]
            {
               "A","B","C","D","E","F",".", " ",
               "G","H","I","J","K","L",",", " ",
               "M", "N","O","P","Q","R","S"," ",
               "T","U","V","W","X","Y","Z"," "
            }, 
            new Vector2(-328, -45),
            50f,
            72f,
            8,
            true,
            125f,
            175f
            )
         );
      var copyUpperCase = new GridData(_interfaceGrids[TypingInputInterface.Uppercase]);
      copyUpperCase.gridValues = new[]
      {
         "a", "b", "c", "d", "e", "f", ".", " ",
         "g", "h", "i", "j", "k", "l", ",", " ",
         "m", "n", "o", "p", "q", "r", "s", " ",
         "t", "u", "v", "w", "x", "y", "z", " "
      }; 
      _interfaceGrids.Add(TypingInputInterface.Lowercase, copyUpperCase);

      _interfaceGrids.Add(TypingInputInterface.Symbols,
         new GridData(new[]
            {
               "0", "1", "2", "3", "4", " ",
               "5", "6", "7", "8", "9", " ",
               "!", "?", "♂", "♀", "/", "-",
               "...", "“", "“", "‘", "‘", " "
            },
            new Vector2(-328, -45),
            90f,
            75f,
            6,
            false)
         );
      
      var letterGridGaps = new[] 
      {
         "A", "B", "C-mid gap", "D", "E", "F-big gap", ".", " ",
         "G", "H", "I-mid gap", "J", "K", "L-big gap", ",", " ",
         "M", "N", "O-mid gap", "P", "Q", "R", "S-mid gap", " ",
         "T", "U", "V-mid gap", "W", "X", "Y", "Z-mid gap", " "
      };
     
      var letterGridData =_interfaceGrids[TypingInputInterface.Uppercase];
      var symbolGridData = _interfaceGrids[TypingInputInterface.Symbols];
      
      symbolGridPositions = new Vector2[symbolGridData.gridSize];
      LoadGridPositions(ref symbolGridPositions, symbolGridData.gridValues,symbolGridData);
      
      letterGridPositions = new Vector2[letterGridData.gridSize];
      LoadGridPositions(ref letterGridPositions, letterGridGaps,letterGridData);
      return;
      void LoadGridPositions(ref Vector2[] positionList,string[] gridValueList,GridData data)
      {
         float currentX = data.startPosition.x;
         float currentY = data.startPosition.y;

         for (int i = 0; i < gridValueList.Length; i++)
         {
            positionList[i] = new Vector2(currentX, currentY);

            float nextGap = data.normalGap;
            
            if (data.hasSpecialGaps)
            {
               if (gridValueList[i].Contains("mid gap"))
                  nextGap = data.midGap;
               else if (gridValueList[i].Contains("big gap"))
                  nextGap = data.bigGap;
            }
            currentX += nextGap;

            // next row
            if ((i + 1) % data.colCount == 0)
            {
               currentX = data.startPosition.x;
               currentY -= data.verticalGap;
            }
         }
      }
   }

   public int GetColumnCount()
   {
      return _interfaceGrids[currentInterface].colCount;
   }
   private void CreateSelectables()
   {
      foreach (var selectable in characterSelectables)
      {
         Destroy(selectable);
      }
      characterSelectables.Clear();
      
      if(currentInterface==TypingInputInterface.Symbols)
      {
         CreateSelectableUi(symbolGridPositions);
      }
      else
      {
         CreateSelectableUi(letterGridPositions);
      }
      void CreateSelectableUi(Vector2[] positions)
      {
         for (var i = 0; i < currentMaxBoxElements; i++)
         {
            var newSelectable = Instantiate(characterPositionTemplate, characterPositionParent);
            
            var rect = newSelectable.GetComponent<RectTransform>();
            rect.anchoredPosition = positions[i];
            
            newSelectable.SetActive(true);
            characterSelectables.Add(newSelectable);
         }
      }
   }
   
   public void InitializeState(int inputLength,TypingInterfaceGraphicData graphicData)
   {
      _dialogueHandler.EndDialogue();
      combinedInput = string.Empty;
      currentInputIndex = 0;
      MaxCharacterLength = inputLength;
      blackArrow.LoadState();
      blackArrow.ChangeActiveState(true);
      interfaceDisplayText.text = graphicData.displayMessage;
      genderGraphic.gameObject.SetActive(false);
      if (graphicData.displayGender != Gender.None)
      {
         genderGraphic.gameObject.SetActive(true);
         genderGraphic.sprite = Utility.GetGenderSprite(graphicData.displayGender);
      }
      
      StartCoroutine(AnimateInterfaceGraphic(graphicData));
      CreateCharacterBoxes();
      ChangeInterface(TypingInputInterface.Uppercase);
      StartCoroutine(_inputStateHandler.PlayTransition(TypingInterfaceNavigation));
      characterBoxes[0].AnimateCharacterBox();
   }

   private IEnumerator AnimateInterfaceGraphic(TypingInterfaceGraphicData graphicData)
   {
      interfaceGraphic.sprite = graphicData.sprites[0];
      Utility.ResizeImageToSprite(ref interfaceGraphic,graphicData.preferredGraphicSize);
      if(!graphicData.isAnimated)
      {
         yield break;
      }
      var currentSpriteIndex = 0;
      while (currentSpriteIndex < graphicData.sprites.Count)
      {
         interfaceGraphic.sprite = graphicData.sprites[currentSpriteIndex];
         yield return new WaitForSeconds(0.25f);
         currentSpriteIndex++;
         if (currentSpriteIndex == graphicData.sprites.Count) currentSpriteIndex = 0;
      }
   }
   private void CreateCharacterBoxes()
   {
      foreach (var box in characterBoxes)
      {
         Destroy(box.gameObject);
      }
      characterBoxes.Clear();

      RectTransform templateRt = characterBoxTemplate.GetComponent<RectTransform>();

      const float preferredWidth = 35f;
      const float gap = 10f;

      float laneWidth = 465f;

      float boxWidth = preferredWidth;

      float preferredTotal =
         MaxCharacterLength * preferredWidth +
         (MaxCharacterLength - 1) * gap;

      if (preferredTotal > laneWidth)
      {
         boxWidth =
            (laneWidth - ((MaxCharacterLength - 1) * gap))
            / MaxCharacterLength;
      }

      float startX =
         templateRt.anchoredPosition.x
         - templateRt.rect.width * 0.5f
         + boxWidth * 0.5f;

      for (int i = 0; i < MaxCharacterLength; i++)
      {
         var newBox =
            Instantiate(characterBoxTemplate, characterBoxParent);

         var typingBox = newBox.GetComponent<TypingCharacterBox>();
         typingBox.LoadBox();
         characterBoxes.Add(typingBox);

         RectTransform rt = newBox.GetComponent<RectTransform>();

         rt.sizeDelta = new Vector2(
            boxWidth,
            rt.sizeDelta.y);

         rt.anchoredPosition = new Vector2(
            startX + i * (boxWidth + gap),
            templateRt.anchoredPosition.y);
      }
   }
   public void TypingInterfaceNavigation()
   {
      CreateSelectables();
      var typingSelectables = new List<SelectableUI>();

      foreach( var selectableObject in characterSelectables)
      {
         typingSelectables.Add( new(selectableObject, InputCharacterValue,true) );
      }
      currentCharacterIndex = 0;
      _inputStateHandler.ChangeInputState(new  (InputStateName.TypingInterfaceNavigation,
         InputStateGroup.TypingInterface,true,mainUI,
         InputDirection.Grid, typingSelectables,characterSelector,true, true ,canExit:false),true);
   }

   public void InterfaceOptionsNavigation()
   {
      var optionSelectables = new List<SelectableUI>
      {
         new(interfaceOptions[0], SwapInterface,true) ,
         new(interfaceOptions[1], ResetCharacterValue,true) ,
         new(interfaceOptions[2], FinalizeInput,true) 
      };
      _inputStateHandler.ChangeInputState(new  (InputStateName.TypingInterfaceOptions,
         InputStateGroup.TypingInterface,false,null,
         InputDirection.Vertical, optionSelectables,optionSelector,true, true ,canExit:false));
   }
   private void ChangeInterface(TypingInputInterface newInterface)
   {
      interfaceImages[(int)currentInterface].gameObject.SetActive(false);
      interfaceImages[(int)newInterface].gameObject.SetActive(true);
      
      currentInterface = newInterface;
      currentMaxBoxElements = _interfaceGrids[currentInterface].gridSize;
   }
   private void SwapInterface()
   {
      if (_isSwappingInterface) return;

      var newInterfaceIndex = (int)currentInterface == 2 ? 0 : (int)currentInterface + 1;
      var newInterface = (TypingInputInterface)newInterfaceIndex;
      StartCoroutine(InterfaceTransitionAnimation(newInterface));
   }

   private IEnumerator InterfaceTransitionAnimation(TypingInputInterface newInterface)
   {
      _isSwappingInterface = true;
      _inputStateHandler.AddChildPlaceHolderState();
      var newImage = interfaceImages[(int)newInterface];
      var newRect = newImage.rectTransform;

      Vector2 originalPos = newRect.anchoredPosition;
      Vector2 raisedPos = originalPos + Vector2.up * 80f;

      newImage.gameObject.SetActive(true);

      // Bring new card to the front
      newRect.SetAsLastSibling();

      // Move up
      yield return StartCoroutine(
         BattleVisuals.SlideRect(
            newRect,
            originalPos,
            raisedPos,
            500f));

      // Move back down
      yield return BattleVisuals.SlideRect(
            newRect,
            raisedPos,
            originalPos,
            500f);
      
      _inputStateHandler.ResetRelevantUi(InputStateName.PlaceHolder,true);
      ChangeInterface(newInterface);
      _isSwappingInterface = false;
   }
   public void SetCurrentCharacterIndex(int newIndex)
   {
      currentCharacterIndex = newIndex;
   }
   private void InputCharacterValue()
   {
      if (currentInputIndex < MaxCharacterLength)
      {
         var selectedCharacter = _interfaceGrids[currentInterface].gridValues[currentCharacterIndex];
         characterBoxes[currentInputIndex].characterText.text = selectedCharacter;
         characterBoxes[currentInputIndex].FreezeCharacterBox();
         combinedInput += selectedCharacter;
         currentInputIndex++;
         OnCharacterCaptured?.Invoke(currentInputIndex);
         if (currentInputIndex < MaxCharacterLength)
         {
            characterBoxes[currentInputIndex].AnimateCharacterBox();
         }
      }
   }

   public void ResetCharacterValue()
   {
      if (currentInputIndex > 0)
      {
         currentInputIndex--;

         characterBoxes[currentInputIndex].characterText.text = string.Empty;
         characterBoxes[currentInputIndex].AnimateCharacterBox();

         if (currentInputIndex + 1 < MaxCharacterLength)
            characterBoxes[currentInputIndex + 1].FreezeCharacterBox();

         combinedInput = combinedInput[..^1];
      }
   }

   private void FinalizeInput()
   {
      if (currentInputIndex == 0)
      {
         _dialogueHandler.DisplayDetails("Input cannot be empty!");
         return;
      }
      StartCoroutine(_inputStateHandler.PlayTransition(CloseInterface));

      void CloseInterface()
      {
         OnInputResolved?.Invoke(combinedInput);
         OnInputResolved = null;
         _inputStateHandler.ResetGroupUi(InputStateGroup.TypingInterface);
         StopAllCoroutines();
      }
     
   }
}
