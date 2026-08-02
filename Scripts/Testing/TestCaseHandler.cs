using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestCase
{
   public int caseIndex;
   public string message;
   public Func<bool> condition;

   public TestCase(int caseIndex,string message, Func<bool> condition)
   {
      this.caseIndex = caseIndex;
      this.message = message;
      this.condition = condition;
   }
}
public class TestCaseHandler
{
   private Dictionary<int,TestCase> testCases = new();
   private TestingEnvironmentHandler _testHandler;
   
   public TestCaseHandler(TestingEnvironmentHandler testHandler)
   {
      _testHandler = testHandler;
   }

   public void AddTestCase(int testCaseIndex,string message,Func<bool> condition)
   {
      testCases.Add(testCaseIndex,new TestCase(testCaseIndex,message,condition));
   }
   private TestCase GetCurrentTestCase(int currentIndex)
   {
      return testCases.FirstOrDefault(c=>c.Key == currentIndex).Value;
   }

   public bool HandleCurrentTestCase(int currentSequenceIndex,Action successCallBack,Action failureCallBack)
   {
      var testCaseResult = GetCurrentTestCase(currentSequenceIndex);
      if (testCaseResult != null)
      {
         if (!testCaseResult.condition.Invoke())
         {
            _testHandler.LogMessage($"Test case({testCaseResult.caseIndex + 1}) Failed due to violation" +
                                    $" of {testCaseResult.message}", TestLogType.Information);
            failureCallBack?.Invoke();
         }
         else
         {
            successCallBack?.Invoke();
         }
         return true;
      }
      return false;
   }
}
