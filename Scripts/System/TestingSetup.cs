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
      _dialogueHandler.OnDialogueDisplayed += LogDialogueMessage;
      
      TestRegistry testRegistry = new();
      
      _gameLoadingHandler.playerData = Resources.Load<PlayerData>(DirectoryHandler
         .GetDirectory(AssetDirectory.TestAssets) + "Test Player");
     
      //Add Tests
      foreach(var integrationTest in testRegistry.allTests)
      {
         integrationTest.testingHandler = this;
         integrationTest.Inject(_container);
         _tests.Add(integrationTest);
      }
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
      DirectoryHandler.ClearDirectory($"Assets/Resources/{DirectoryHandler.GetDirectory(AssetDirectory.TestLogs)}");
      yield return new WaitForSeconds(2f);
      foreach (var test in _tests)
      {
         LogMessage($"<- {test.testName} -> has begun",TestLogType.Test);
         test.onTestResult += GetTestFeedBack;
         yield return StartCoroutine(test.BeginTest());
         if (test.testStatus == IntegrationTest.TestStatus.Failed)
         {
            break;
         }
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
          DirectoryHandler.GetDirectory(AssetDirectory.TestLogs), "Full Logs.html");
      
      string htmlHeader = @"<!DOCTYPE html>
      <html lang=""en"">
      <head>
      <meta charset=""UTF-8"">
      <title>Test Log</title>

      <style>
      body{
          background:#1e1e1e;
          color:#d4d4d4;
          font-family:Consolas, monospace;
          margin:20px;
      }

      h1{
          color:white;
      }

      table{
          width:100%;
          border-collapse:collapse;
      }

      th{
          text-align:left;
          padding:8px;
          border-bottom:2px solid #666;
          color:white;
      }

      td{
          padding:4px 8px;
          border-bottom:1px solid #333;
          vertical-align:top;
      }

      .time{
          color:#888;
          width:220px;
      }

      .type{
          width:120px;
          font-weight:bold;
      }

    .dialogue{
    color:#dcdcaa;      /* Soft yellow - character dialogue */
    }

    .health{
        color:#800080;      /* Red - HP/healing/damage */
    }

    .calculation{
        color:#c586c0;      /* Purple - formulas and calculations */
    }

    .information{
        color:#58a6ff;      /* Blue - general information */
    }

    .error{
        color:#f85149;      /* Bright red - errors */
        font-weight:bold;
    }

    .test{
        color:#79c0ff;      /* Cyan - test start/status */
        font-weight:bold;
    }

    .pass{
        color:#3fb950;      /* Green - successful test */
        font-weight:bold;
    }

      .separator td{
          border-bottom:2px solid #666;
      }
      </style>

      </head>

      <body>

      <h1>Test Logs</h1>

      <table>

      <tr>
          <th>Timestamp</th>
          <th>Type</th>
          <th>Message</th>
      </tr>";

      string htmlFooter = @"
      </table>

      </body>
      </html>";

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
      
      string html = htmlHeader + rows + htmlFooter;
      File.WriteAllText(baseDir, html);
   }
}

public struct MessageLog
{
   public DateTime timestamp;
   public TestLogType type;
   public string message;
   public MessageLog(DateTime timestamp, string message,TestLogType type)
   {
      this.timestamp = timestamp;
      this.message = message;
      this.type = type;
   }
}
public enum TestLogType
{
   Dialogue,
   Health,
   Calculation,
   Information,
   Error,
   Test,
   Pass
}
public abstract class IntegrationTest
{
   public TestingSetup testingHandler;
   public string testName;
   public enum TestStatus{Passed,Failed}
   public TestStatus testStatus;
   public Action onTestResult;
   public virtual IEnumerator BeginTest()
   {
      yield return null;
   }
   public virtual void Inject(ServiceContainer container) { }

   public void SetStatus(bool condition)
   {
      testStatus = condition ? TestStatus.Passed : TestStatus.Failed;
   }
}
public class TestRegistry
{
   //tests are ran in this order
   public IntegrationTest[] allTests =
   {
      //Move Based Tests
      new CreateBarrierMoveTest(),
      new HealthDrainTest(),
      new HealFromWeatherTest(),
      new DamageProtectionMoveTest(),
      new WeatherChangeTest(),
      new ConsecutiveMoveTest(),
      new TargetAllExceptSelfTest()
   };
}
