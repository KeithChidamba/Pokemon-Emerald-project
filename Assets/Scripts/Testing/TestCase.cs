using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class TestCase
{
    public int caseIndex;
    public List<TestCaseCondition> conditions;

    public TestCase(int caseIndex, List<TestCaseCondition> conditions)
    {
        this.caseIndex = caseIndex;
        this.conditions = conditions;
    }

    public string GetCaseMessages()
    {
        StringBuilder rows = new();
        foreach (var condition in conditions)
        {
            rows.AppendLine(condition.message);
        }
        return rows.ToString();
    }
}

public class TestCaseCondition
{
    public string message;
    public Func<bool> requirement;
    public TestCaseCondition(string message, Func<bool> requirement)
    {
        this.message = message;
        this.requirement = requirement;
    }
}