using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

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
   private EndToEndTest currenEndToEndTest;
   
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

   public void ValidateEndToEndTest()
   {
       if (currenEndToEndTest == null)
       {
           Debug.LogError("Can't use that right now. Only for use during [End-To-End] tests");
           return;
       }
       currenEndToEndTest.ValidateTestAndEnd(this);
   }
   private void LogDialogueMessage(string newMessage)
   {
      LogMessage(newMessage,TestLogType.Dialogue);
   }
   public void LogMessage(string newMessage,TestLogType type)
   {
      testingLogs.Add(NextLogID,new MessageLog(DateTime.Now,newMessage,type));
   }
   
   private IEnumerator RunTests()
   {
       TestRegistry testRegistry = new();
       yield return new WaitForSeconds(0.1f);
       
       DirectoryHandler.ClearDirectory(Path.Combine("Assets/Resources", 
           DirectoryHandler.GetDirectory(AssetDirectory.TestLogs)));

       //End to End Tests
       foreach (var endToEndTest in testRegistry.endToEndTests)
       {
           currenEndToEndTest = endToEndTest;
           
           endToEndTest.Inject(_container);
           yield return new WaitForSeconds(0.5f);
            
           LogMessage($"Running [End-to-End] Test {endToEndTest.testName}",TestLogType.Test);
            
           var testData = Resources.Load<EndToEndTestData>(
               DirectoryHandler.GetDirectory(AssetDirectory.Tests) + $"{endToEndTest.testName}/Test Data");
            
           _dialogueHandler.DisplayObjectiveText(testData.testDescription);
           
           var pokemonOperationsHandler = _container.Resolve<PokemonOperations>();
           var pokemonPartyHandler = _container.Resolve<PokemonPartyHandler>();
           yield return TestingUtilities.LoadPokemonPartyTestData(testData.pokemonPartyData,pokemonPartyHandler,pokemonOperationsHandler);
           _dialogueHandler.EndDialogue();
           
           yield return endToEndTest.BeginTest(testData);
           endToEndTest.testOperationStarted = true;
           yield return new WaitUntil(()=>endToEndTest.testOperationsComplete);
            
           var testResult = endToEndTest.testStatus == TestStatus.Passed? "passed":"failed";
            
           LogMessage($"[End-to-End] Test {testResult}",endToEndTest.testStatus == TestStatus.Passed
               ? TestLogType.Pass:TestLogType.Error);
            
           if (endToEndTest.testStatus == TestStatus.Failed)
           { 
               Debug.LogWarning($"-------------TEST FAILED---------------");
               break;
           }
           yield return new WaitForSeconds(0.01f);
       }
       GetLogs("End To End Test Logs.html"); 
       testingLogs.Clear();
       Debug.Log($"[End To End] TEST LOGS PRINTED");
       yield return new WaitForSeconds(1f);
       
       //Unit Tests
       // var unitTestHandler = new UnitTestHandler(_container);
       // yield return unitTestHandler.RunTests();
       // GetLogs("Unit Test Logs.html"); 
       // testingLogs.Clear();
       // Debug.Log($"UNIT TEST LOGS PRINTED");
       // yield return new WaitForSeconds(5f);
       
       //Integration Tests
       
       foreach(var test in testRegistry.integrationTests)
       {
           test.testingHandler = this;
           test.Inject(_container);
           
           yield return new WaitForSeconds(0.5f);
           
           LogMessage($"<- {test.testName} -> has begun",TestLogType.Test);
           test.onTestResult += GetTestFeedBack;
           yield return new WaitForSeconds(0.01f);
           yield return StartCoroutine(test.BeginTest());
           if (test.testStatus == TestStatus.Failed)
           { 
               Debug.LogWarning($"-------------TEST FAILED---------------");
               break;
           }
           continue;
           void GetTestFeedBack()
           { 
               test.onTestResult -= GetTestFeedBack; 
               LogMessage($"<- {test.testName} -> has {test.testStatus}"
                   ,test.testStatus == TestStatus.Failed?
                       TestLogType.Error:TestLogType.Pass);
           }
       }
       GetLogs("Integration Test Logs.html"); 
       Debug.Log($"Integration TEST LOGS PRINTED");
   }
   private void GetLogs(string logName)
   {
      var baseDir = Path.Combine("Assets/Resources", 
          DirectoryHandler.GetDirectory(AssetDirectory.TestLogs), logName);
      
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
    public EndToEndTest[] endToEndTests =
    {
        // new RareCandyTest(),
        new FriendshipBerryTest(),
    };
    
    public List<IntegrationTest> integrationTests = new()
    { 
    //Held Items
        // new ConsumableHeldItemUsageTest(),
        // new ChoiceBandTest(),
    //Special Move Logic
        // new BideTest(),
        // new HyperBeamTest(),
        // new MirrorMoveTest(),
        // new SilverwindBattleEndTest(),
        // new SilverwindSwapTest(),
        // new WhirlwindWildBattleTest(),
        // new WhirlwindTrainerBattleTest(),
        // new WhirlwindDoubleBattleTest(),
        // new ThunderTest(),
        // new Endeavor(),
        // new RestTest(),
        // new BellyDrumTest(),
        // new CovetTest(),
        // new FalseSwipeTest(),
        // new FlailTest(),
        // new FuryCutter(),
        // new TakeDownTest(),
        // new HazeTest(),
        // new PursuitTest(),
        // new BrickBreakTest(),
    //Abilities
        // new HealthBasedDamageBuffTest(),
        // new StatusEffectDamageBuffTest(),
        // new ShedSkinTest(),
        // new StaticTest(),
        // new ArenaTrapTest(),
        // new LevitateTest(),
        // new GutsTest(),
        // new PickupTest(),
        // new InnerFocusTest(),
    //Battle system tests
        // new TrapEffectTest(),
        // new InfatuationEffectTest(),
        // new FlinchEffectTest(),
        // new StruggleTest(),
        // new StatChangeApplicationTest(),
        // new StatusEffectTest(),
        // new WeatherDamageTest(),
        // new OnFieldDamageModificationTest(),
    //Move Based Tests
        // new SpecificMoveDamageTest(),
        // new SemiInvulnerableSingleBattleTest(),
        // new SemiInvulnerableDoubleBattleTest(),
        // new IdentifyTargetMoveTest(),
        // new MultiTargetDamageTest(),
        // new CreateBarrierMoveTest(),
        // new HealthDrainTest(),
        // new HealFromWeatherTest(),
        // new DamageProtectionMoveTest(),
        // new ConsecutiveMoveTest()
    };
}

