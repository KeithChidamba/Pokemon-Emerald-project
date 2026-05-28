using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public enum TypingInputInterface
{
   Uppercase = 0,Lowercase = 1,Symbols = 2
}
public class TypingInterfaceHandler : MonoBehaviour,IInjectable
{
   public GameObject mainUI;
   public GameObject characterSelector;
   public GameObject optionSelector;
   [SerializeField]private GameObject[] interfaceOptions;
   [SerializeField]private LoopingUiAnimation blackArrow;
   [SerializeField]private Image[] characterBoxes;
   [SerializeField]private List<TMP_Text> characterTexts;
   [SerializeField]private Image[] interfaceImages;
   public TypingInputInterface currentInterface;
   public int currentCharacterIndex;
   [SerializeField]private int _maxCharacterLength = 7;
   public int currentMaxBoxElements;
   [SerializeField]private string _combinedInput;
   private Dictionary<TypingInputInterface, string[]> _interfaceCharacters = new();
   [SerializeField]private List<GameObject> characterSelectables;
   [SerializeField]private GameObject characterPositionTemplate;
   [SerializeField]private Transform characterPositionParent;
   public event Action<string> OnInputResolved;

   [SerializeField]private Vector2[] symbolGridPositions;
   [SerializeField]private Vector2[] letterGridPositions;
   
   private InputStateHandler _inputStateHandler;
   private Game_ui_manager _gameUIHandler;
   
   public void Inject(ServiceContainer container)
   {
      _inputStateHandler = container.Resolve<InputStateHandler>();
      _gameUIHandler = container.Resolve<Game_ui_manager>();
      gameObject.SetActive(true);
   }

   public void OnInject()
   {
      blackArrow.viewingUI = true;
      foreach (var box in characterBoxes)
      {
         characterTexts.Add(box.GetComponentInChildren<TMP_Text>());
      }
      symbolGridPositions = new Vector2[]
      {
         new(-328, -45), new(-278, -45), new(-228, -45), new(-178, -45), new(-128, -45), new(-78, -45),

         new(-328, 27),  new(-278, 27),  new(-228, 27),  new(-178, 27),  new(-128, 27),  new(-78, 27),

         new(-328, 99),  new(-278, 99),  new(-228, 99),  new(-178, 99),  new(-128, 99),  new(-78, 99),

         new(-328, 171), new(-278, 171), new(-228, 171), new(-178, 171), new(-128, 171), new(-78, 171)
      };
      var letters = new[]
      {
         "A", "B", "C-mid gap", "D", "E", "F-big gap", " ", ".",
         "G", "H", "I-mid gap", "J", "K", "L-big gap", " ", ",",
         "M", "N", "O-mid gap", "P", "Q", "R", "S-mid gap", " ",
         "T", "U", "V-mid gap", "W", "X", "Y", "Z-mid gap", " "
      };
      var basePosition = new Vector2(-328, -45);
      var normalGap = 50f;
      var midGap = 125f;
      var bigGap = 175f;
      var verticalGap = 72f;
      var colCount = GetColumnCount();
      letterGridPositions = new Vector2[letters.Length];

      float currentX = basePosition.x;
      float currentY = basePosition.y;

      for (int i = 0; i < letters.Length; i++)
      {
         letterGridPositions[i] = new Vector2(currentX, currentY);

         float nextGap = normalGap;

         if (letters[i].Contains("mid gap"))
            nextGap = midGap;
         else if (letters[i].Contains("big gap"))
            nextGap = bigGap;

         currentX += nextGap;

         // next row
         if ((i + 1) % colCount == 0)
         {
            currentX = basePosition.x;
            currentY -= verticalGap;
         }
      }

      _interfaceCharacters.Add(TypingInputInterface.Uppercase,       
      new[]
      {
         "A","B","C","D","E","F"," ", ".",
         "G","H","I","J","K","L"," ", ",",
         "M", "N","O","P","Q","R","S"," ",
         "T","U","V","W","X","Y","Z"," "
      });
      _interfaceCharacters.Add(TypingInputInterface.Lowercase, 
      new[]
      {
         "a","b","c","d","e","f"," ", ".",
         "g","h","i","j","k","l"," ", ",",
         "m","n","o","p","q","r","s"," ",
         "t","u","v","w","x","y","z"," "
      });
      _interfaceCharacters.Add(TypingInputInterface.Symbols,
      new[]
      {
         "0","1","2","3","4"," ",
         "5","6","7","8","9", " ",
         "!", "?","♂","♀","/","-",
         "...","“","“","‘","‘"," "
      });
   }

   public int GetColumnCount()
   {
      return currentInterface switch
      {
         TypingInputInterface.Symbols => 6,
         _ => 8
      };
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
   
   public void InitializeState()
   {
      foreach (var text in characterTexts)
      {
         text.text = string.Empty;
      }
      _combinedInput = string.Empty;
      currentCharacterIndex = 0;
      ChangeInterface(TypingInputInterface.Uppercase,false);
      TypingInterfaceNavigation();
   }

   public void TypingInterfaceNavigation()
   {
      CreateSelectables();
      var typingSelectables = new List<SelectableUI>();

      for (int i = 0; i<characterSelectables.Count; i++)
      {
         typingSelectables.Add( new(characterSelectables[i], InputCharacterValue,true) );
      }
        
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
      currentMaxBoxElements = currentInterface switch
      {
         TypingInputInterface.Symbols => 24,
         _ => 32
      };
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
   
   private void InputCharacterValue()
   {
      if (currentCharacterIndex < _maxCharacterLength - 1)
      {
         characterTexts[currentCharacterIndex].text = _interfaceCharacters[currentInterface][currentCharacterIndex];
         _combinedInput += characterTexts[currentCharacterIndex].text;
         currentCharacterIndex++;
      }
   }

   private void ResetCharacterValue()
   {
      if (currentCharacterIndex > 0)
      {
         characterTexts[currentCharacterIndex-1].text = string.Empty;
         _combinedInput = _combinedInput[..^1];//remove last input
         currentCharacterIndex--;
      }
   }
   public void FreezeCharacterBox()
   {
      
   }
   public void AnimateCharacterBox()
   {
      
   }

   private void FinalizeInput()
   {
      OnInputResolved?.Invoke(_combinedInput);
      OnInputResolved = null;
      _gameUIHandler.CloseTypingInterface();
      _inputStateHandler.ResetGroupUi(InputStateGroup.TypingInterface);
   }
}
