using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public enum DevelopmentEnvironment
{
   Testing,Production
}

public class TestingEnvironmentHandler : MonoBehaviour,IInjectable
{ 
   public DevelopmentEnvironment environment;
   private Dictionary<int, MessageLog> testingLogs = new();
   private int NextLogID => testingLogs.Count+1;
   private TestingUtilities _testUtils;
   
   private DialogueHandler _dialogueHandler;
   private GameLoadingHandler _gameLoadingHandler;
   private ServiceContainer _container;
   
   public void Inject(ServiceContainer container)
   {
      _dialogueHandler = container.Resolve<DialogueHandler>();
      _gameLoadingHandler = container.Resolve<GameLoadingHandler>();
      _container = container;
      gameObject.SetActive(true);
   }
   public void OnInject()
   {
       _testUtils = new TestingUtilities();
      //logging
      _dialogueHandler.OnDialogueDisplayed += LogDialogueMessage;
      
      _gameLoadingHandler.playerData = Resources.Load<PlayerData>(DirectoryHandler
         .GetDirectory(AssetDirectory.TestAssets) + "Test Player");
      
      _gameLoadingHandler.StartGame(false);
      StartCoroutine(RunTests());
   }
   private void LogDialogueMessage(string newMessage)
   {
      LogMessage(newMessage,TestLogType.Dialogue);
   }
   public void LogMessage(string newMessage,TestLogType type)
   {
      testingLogs.Add(NextLogID,new(DateTime.Now,newMessage,type));
   }
   
   private IEnumerator RunTests()
   {
       DirectoryHandler.ClearDirectory(Path.Combine("Assets/Resources", 
           DirectoryHandler.GetDirectory(AssetDirectory.TestLogs)));
       
       TestRegistry testRegistry = new();
       foreach (var test in testRegistry.allTests)
       {
           test.testingHandler = this;
           test.Inject(_container);
       }
       
       yield return new WaitForSeconds(2f);
      
       foreach(var test in testRegistry.allTests)
       {
           LogMessage($"<- {test.testName} -> has begun",TestLogType.Test);
           test.onTestResult += GetTestFeedBack;
           yield return new WaitForSeconds(0.01f);
           yield return StartCoroutine(test.BeginTest());
           if (test.testStatus == IntegrationTest.TestStatus.Failed)
           { 
               break;
           }
           continue;
           void GetTestFeedBack()
           { 
               test.onTestResult -= GetTestFeedBack; 
               LogMessage($"<- {test.testName} -> has {test.testStatus}"
                   ,test.testStatus == IntegrationTest.TestStatus.Failed?
                  TestLogType.Error:TestLogType.Pass);
           }
       } 
       GetLogs(); 
       Debug.Log($"TEST LOGS PRINTED");
   }
   
   private void GetLogs()
   {
      var baseDir = Path.Combine("Assets/Resources", 
          DirectoryHandler.GetDirectory(AssetDirectory.TestLogs), $"Full Logs {Utility.Random16Bit()}.html");
      
      StringBuilder rows = new();
      foreach (var log in testingLogs.Values)
      {
         rows.AppendLine($@"
        <tr>
            <td class=""time"">{log.timestamp}</td>
            <td class=""type {log.type.ToString().ToLowerInvariant()}"">{log.type}</td>
            <td>{System.Net.WebUtility.HtmlEncode(log.message)}</td>
        </tr>");
      }
      
      string html =  _testUtils.htmlHeader + rows + _testUtils.htmlFooter;
      File.WriteAllText(baseDir, html);
   }
}
public class TestRegistry
{
    //tests are ran in this order
    public IntegrationTest[] allTests =
    {
       
        //Move Based Tests
        new StatusEffectTest(),
        //new SemiInvulnerableDoubleBattleTest(),
        //new SemiInvulnerableSingleBattleTest(),
        // new WeatherDamageTest(),
        // new WeatherDamageTest(),
        // new MultiTargetDamageTest(),
        // new OnFieldDamageModificationTest(),
        // new IdentifyTargetMoveTest(),
        // new CreateBarrierMoveTest(),
        // new HealthDrainTest(),
        // new HealFromWeatherTest(),
        // new DamageProtectionMoveTest(),
        // new ConsecutiveMoveTest(),
    };
}

