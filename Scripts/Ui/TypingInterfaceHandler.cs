using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TypingInputInterface
{
   Uppercase = 0,Lowercase = 1,Symbols = 2
}
public class TypingInterfaceHandler : MonoBehaviour,IInjectable
{
   public LoopingUiAnimation blackArrow;
   public Image[] characterBoxes;
   public Text[] characterTexts;
   public Image[] interfaceImages;
   public TypingInputInterface currentInterface;
   public int currentCharacterIndex;
   private int _maxCharacterLength;
   private Dictionary<TypingInputInterface, string[]> _interfaceCharacters = new();

   public void Inject(ServiceContainer container)
   {
      
   }

   public void OnInject()
   {
      blackArrow.viewingUI = true;
      
      _interfaceCharacters.Add(TypingInputInterface.Uppercase,       
      new[]
      {
         "A","B","C","D","E","F","G","H","I","J","K","L","M",
         "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
         ".",","
      });
      _interfaceCharacters.Add(TypingInputInterface.Lowercase, 
      new[]
      {
         "a","b","c","d","e","f","g","h","i","j","k","l","m",
         "n","o","p","q","r","s","t","u","v","w","x","y","z",
         ".",","
      });
      _interfaceCharacters.Add(TypingInputInterface.Symbols,
      new[]
      {
         "0","1","2","3","4","5","6","7","8","9",
         "!","?","/","...","\"","'","-","♂","♀"
      });
   }

   public void ChangeInterface(TypingInputInterface newInterface)
   {
      interfaceImages[(int)currentInterface].gameObject.SetActive(false);
      currentInterface = newInterface;
      interfaceImages[(int)newInterface].gameObject.SetActive(true);
   }
   public void SetCharacterValue(int index)
   {
      if (currentCharacterIndex < _maxCharacterLength - 1)
      {
         characterTexts[index].text = _interfaceCharacters[currentInterface][index];
         currentCharacterIndex++;
      }
   }

   public void ResetCharacterValue(int index)
   {
      if (currentCharacterIndex > 0)
      {
         characterTexts[index].text = string.Empty;
         currentCharacterIndex--;
      }
   }
   public void FreezeCharacterBox()
   {
      
   }
   public void AnimateCharacterBox()
   {
      
   }
   
}
