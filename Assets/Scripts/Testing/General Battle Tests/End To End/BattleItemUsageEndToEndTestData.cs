using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "test", menuName = "Tests/End To End/Battle Item Usage End To End test data")]
public class BattleItemUsageEndToEndTestData : EndToEndTestData
{
    public List<TestItem> testItems = new();
    public TestTrainerData testEnemyData;
}
