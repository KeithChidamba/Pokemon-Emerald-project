using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "test", menuName = "Tests/End To End/Item End To End test data")]
public class ItemEndToEndTestData : EndToEndTestData
{
    public List<TestItem> testItems = new();
}
[Serializable]
public struct TestItem
{
    public Item itemAsset;
    public int quantity;
}