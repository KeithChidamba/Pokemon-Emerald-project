using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ModifyPowerpointsTest: ItemEndToEndTest
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private TestingEnvironmentHandler _testHandler;
    private GameUiHandler _gameUiHandler;

    private int powerPointIncrease;
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var itemData = (ItemEndToEndTestData)testData;
        LoadItems(itemData.testItems);

        var currentMove = _pokemonPartyHandler.Party[0].moveSet[0];
        var maxPp = Mathf.FloorToInt(_pokemonPartyHandler.Party[0].moveSet[0].basePowerpoints * 1.6f);
        powerPointIncrease = Mathf.FloorToInt(0.2f * currentMove.basePowerpoints);
        _testHandler.LogMessage($"max PP amount : {maxPp}",TestLogType.Calculation);
        _testHandler.LogMessage($"PP increrase : {powerPointIncrease}",TestLogType.Calculation);
        _testHandler.LogMessage($"base PP : {currentMove.basePowerpoints}",TestLogType.Calculation);
        _testHandler.LogMessage($"current Max PP : {currentMove.maxPowerpoints}",TestLogType.Calculation);
        
        //test PP Up
        AddTestCaseScenario(()=>
        {
            currentMove.maxPowerpoints = maxPp;
            _testHandler.LogMessage($"scenario max: {currentMove.maxPowerpoints}",TestLogType.Information);
        });
        AddTestCaseScenario(()=>
        {
            currentMove.maxPowerpoints = currentMove.basePowerpoints;
            _testHandler.LogMessage($"scenario max: {currentMove.maxPowerpoints}",TestLogType.Information);
        });
        
        //Test PP max
        AddTestCaseScenario(()=>
        {
            currentMove.maxPowerpoints = maxPp;
            _testHandler.LogMessage($"scenario max: {currentMove.maxPowerpoints}",TestLogType.Information);
        });
        AddTestCaseScenario(()=>
        {
            currentMove.maxPowerpoints = currentMove.basePowerpoints + powerPointIncrease;
            _testHandler.LogMessage($"scenario max: {currentMove.maxPowerpoints}",TestLogType.Information);
        });
        
        AddTestCase("PP up usage should fail fail due to pp being at max",() => 
            currentMove.maxPowerpoints == maxPp);
        AddTestCase("PP up usage should work",() => 
            currentMove.maxPowerpoints == currentMove.basePowerpoints + powerPointIncrease);
        
        AddTestCase("PP max usage should fail fail due to pp being at max",() => 
            currentMove.maxPowerpoints == maxPp);
        AddTestCase("PP max usage should work",() => 
            currentMove.maxPowerpoints == maxPp);
        _gameUiHandler.ValidateBagView();
        yield return null;
    }
    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Modify Powerpoints Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _testHandler =  container.Resolve<TestingEnvironmentHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
    }
}
