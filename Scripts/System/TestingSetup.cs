using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum DevelopmentEnvironment
{
   Testing,Production
}

public enum TestType
{
   ItemUsage,BattleMoveUsage
}
public class TestingSetup : MonoBehaviour,IInjectable
{ 
   public DevelopmentEnvironment environment;
   private List<IntegrationTest> _tests = new();
   private Dictionary<int, MessageLog> testingLogs = new();
   private int NextLogID => testingLogs.Count+1;
   
   private Dialogue_handler _dialogueHandler;
   private Game_Load _gameLoadingHandler;
   private ServiceContainer _container;
   
   public void Inject(ServiceContainer container)
   {
      _dialogueHandler = container.Resolve<Dialogue_handler>();
      _gameLoadingHandler = container.Resolve<Game_Load>();
      _container = container;
      gameObject.SetActive(true);
   }
   public void OnInject()
   {
      //logging
      _dialogueHandler.OnDialogueDisplayed += LogMessages;
      var playerTestData = Resources.Load<PlayerData>(DirectoryHandler.GetDirectory(AssetDirectory.TestAssets)+"Test Player");
      _gameLoadingHandler.playerData = playerTestData;
      Debug.Log(_gameLoadingHandler.playerData.playerName);
      //Add Tests
      var allTests = Resources.LoadAll<TestingData>(DirectoryHandler.GetDirectory(AssetDirectory.TestAssets));
      foreach(var testData in allTests)
      {
         var newTestLogicHandler = CreateTestOfType(testData.testType);
         newTestLogicHandler.testName = testData.testName;
         newTestLogicHandler.Inject(_container,testData);
         _tests.Add(newTestLogicHandler);
      }
      Debug.Log(_tests.Count);
      _gameLoadingHandler.StartGame(false);
      StartCoroutine(RunTests());
      void LogMessages(string newMessage)
      {
         testingLogs.Add(NextLogID,new(DateTime.Now,newMessage));
      }
   }



   private void GetLogs()
   {
      var baseDir = "Assets/Resources/" + DirectoryHandler.GetDirectory(AssetDirectory.TestLogs) + "NewLogs.txt";
      using (StreamWriter writer = new StreamWriter(baseDir))
      {
         foreach (var log in testingLogs)
         {
            var logString = $"{log.Value.timestamp} : {log.Value.message}";
            writer.WriteLine(logString);
         }
      }
   }

   private IEnumerator RunTests()
   {
      yield return new WaitForSeconds(2f);
      foreach (var test in _tests)
      {
         test.onTestResult += GetTestFeedBack;
         yield return StartCoroutine(test.BeginTest());
         if (test.testStatus == IntegrationTest.TestStatus.Failed)
         {
            break;
         }
         void GetTestFeedBack()
         {
            test.onTestResult -= GetTestFeedBack;
            var result = test.testStatus == IntegrationTest.TestStatus.Failed? "Failed" : "Passed";
            Debug.Log($"{test.testName} has {result}");
         }
      }
      GetLogs();
   }
   private static IntegrationTest CreateTestOfType(TestType type)
   {
      return type switch
      {
         TestType.ItemUsage => new ItemUsageTest(),
         TestType.BattleMoveUsage => new BattleMoveUsageTest(),
         _ => null
      };
   }
}

public struct MessageLog
{
   public DateTime timestamp;
   public string message;
   public MessageLog(DateTime timestamp, string message)
   {
      this.timestamp = timestamp;
      this.message = message;
   }
}
public abstract class IntegrationTest
{
   public string testName;
   public enum TestStatus{Passed,Failed}
   public TestStatus testStatus;
   public Action onTestResult;
   public virtual IEnumerator BeginTest()
   {
      yield return null;
   }
   public virtual void Inject(ServiceContainer container, TestingData data) { }
}

