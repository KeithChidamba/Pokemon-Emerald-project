using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TypingInputInterface
{
   Uppercase = 0,Lowercase = 1,Symbols = 2
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
      private int _maxCharacterLength;
      
   [SerializeField]private Image[] interfaceImages;
   public TypingInputInterface currentInterface;
   [SerializeField]private int currentInputIndex;
   public int currentCharacterIndex;
   public int currentMaxBoxElements;
   [SerializeField]private string combinedInput;
   
   private Dictionary<TypingInputInterface, GridData> _interfaceGrids = new();
   [SerializeField]private List<GameObject> characterSelectables;
   [SerializeField]private GameObject characterPositionTemplate;
   [SerializeField]private Transform characterPositionParent;
   public event Action<string> OnInputResolved;

   [SerializeField]private Vector2[] symbolGridPositions;
   [SerializeField]private Vector2[] letterGridPositions;
   
   private InputStateHandler _inputStateHandler;
   private Game_ui_manager _gameUIHandler;
   private Dialogue_handler _dialogueHandler;
   
   public void Inject(ServiceContainer container)
   {
      _inputStateHandler = container.Resolve<InputStateHandler>();
      _gameUIHandler = container.Resolve<Game_ui_manager>();
      _dialogueHandler = container.Resolve<Dialogue_handler>();
      gameObject.SetActive(true);
   }
   
   public void OnInject()
   {
      _interfaceGrids.Add(TypingInputInterface.Uppercase,   
         new GridData(
            new[]
            {
               "A","B","C","D","E","F"," ", ".",
               "G","H","I","J","K","L"," ", ",",
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
         "a", "b", "c", "d", "e", "f", " ", ".",
         "g", "h", "i", "j", "k", "l", " ", ",",
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
         "A", "B", "C-mid gap", "D", "E", "F-big gap", " ", ".",
         "G", "H", "I-mid gap", "J", "K", "L-big gap", " ", ",",
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
   
   public void InitializeState(int inputLength)
   {
      combinedInput = string.Empty;
      currentInputIndex = 0;
      _maxCharacterLength = inputLength;
      blackArrow.LoadState();
      blackArrow.ChangeActiveState(true);
      CreateCharacterBoxes();
      ChangeInterface(TypingInputInterface.Uppercase,false);
      TypingInterfaceNavigation();
      characterBoxes[0].AnimateCharacterBox();
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
         _maxCharacterLength * preferredWidth +
         (_maxCharacterLength - 1) * gap;

      if (preferredTotal > laneWidth)
      {
         boxWidth =
            (laneWidth - ((_maxCharacterLength - 1) * gap))
            / _maxCharacterLength;
      }

      float startX =
         templateRt.anchoredPosition.x
         - templateRt.rect.width * 0.5f
         + boxWidth * 0.5f;

      for (int i = 0; i < _maxCharacterLength; i++)
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
   private void ChangeInterface(TypingInputInterface newInterface,bool refreshState)
   {
      interfaceImages[(int)currentInterface].gameObject.SetActive(false);
      interfaceImages[(int)newInterface].gameObject.SetActive(true);
      
      currentInterface = newInterface;
      currentMaxBoxElements = _interfaceGrids[currentInterface].gridSize;
      
      if (!refreshState) return;
      _inputStateHandler.ResetRelevantUi(InputStateName.TypingInterfaceNavigation);
   }
   private void SwapInterface()
   {
      var newInterfaceIndex = (int)currentInterface == 2 ? 0 : (int)currentInterface + 1;
      var newInterface = newInterfaceIndex switch
      {
         0 => TypingInputInterface.Uppercase,
         1 => TypingInputInterface.Lowercase,
         _ => TypingInputInterface.Symbols
      };
      ChangeInterface(newInterface,true);
   }

   public void SetCurrentCharacterIndex(int newIndex)
   {
      currentCharacterIndex = newIndex;
   }
   private void InputCharacterValue()
   {
      if (currentInputIndex < _maxCharacterLength)
      {
         var selectedCharacter = _interfaceGrids[currentInterface].gridValues[currentCharacterIndex];
         characterBoxes[currentInputIndex].characterText.text = selectedCharacter;
         characterBoxes[currentInputIndex].FreezeCharacterBox();
         combinedInput += selectedCharacter;
         currentInputIndex++;

         if (currentInputIndex < _maxCharacterLength)
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

         if (currentInputIndex + 1 < _maxCharacterLength)
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
      OnInputResolved?.Invoke(combinedInput);
      OnInputResolved = null;
      _gameUIHandler.CloseTypingInterface();
      _inputStateHandler.ResetGroupUi(InputStateGroup.TypingInterface);
   }
}
