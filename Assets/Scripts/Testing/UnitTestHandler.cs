using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitTestHandler
{
    public enum UnitTestName{MagnitudeDamageTest,PickupAbilityItemPoolTest}

    public Dictionary<UnitTestName, Func<IEnumerator>> unitTests = new();

    private MoveLogicDatabase _moveLogicDatabase;
    private PokemonOperations _pokemonOperationsHandler;
    private TestingEnvironmentHandler _testingHandler;

    private bool _currentTestPassed;
    public UnitTestHandler(ServiceContainer container)
    {
        _moveLogicDatabase= container.Resolve<MoveLogicDatabase>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        _testingHandler = container.Resolve<TestingEnvironmentHandler>();
            
        unitTests.Add(UnitTestName.MagnitudeDamageTest,MagnitudeDamageTest);
        unitTests.Add(UnitTestName.PickupAbilityItemPoolTest,PickupAbilityItemPoolTest);
    }

    public IEnumerator RunTests()
    {
        foreach (var test in unitTests)
        {
            _testingHandler.LogMessage($"Running Unit Test {test.Key}",TestLogType.Test);
            yield return test.Value.Invoke();
            
            var testResult = _currentTestPassed? "passed":"failed";
            _testingHandler.LogMessage($"Unit Test {testResult}",_currentTestPassed? TestLogType.Pass:TestLogType.Error);
            yield return new WaitForSeconds(0.01f);
        }
    }
    public IEnumerator MagnitudeDamageTest()
    {
        List<(int strength, float expectedDamage)> damageLevels = new()
        {
            (4, 10f),
            (5, 30f),
            (6, 50f),
            (7, 70f),
            (8, 90f),
            (9, 110f),
            (10, 150f)
        };
        for (int i = 4; i < 11; i++)
        {
            var currentMagnitudeStrength = i;
            var damage = _moveLogicDatabase.MagnitudeDamageEffect(currentMagnitudeStrength);
            _testingHandler.LogMessage($"Magnitude strength {currentMagnitudeStrength}, has damage " +
                                       $"{damage}",TestLogType.TestCase);
            var damageForLevel = damageLevels.First(s => s.strength == currentMagnitudeStrength).expectedDamage;
            
            _currentTestPassed = Mathf.FloorToInt(damage) == Mathf.FloorToInt(damageForLevel);
        }
        yield return null;
    }
    private IEnumerator PickupAbilityItemPoolTest()
    {
        bool testComplete = false;
        var pokemonAsset = Resources.Load<Pokemon>(DirectoryHandler.GetDirectory(AssetDirectory.Pokemon) + "Zigzagoon/Zigzagoon");
        yield return _pokemonOperationsHandler.HandlePokemonCreation(CreateMember,pokemonAsset,5,1);
        yield return new WaitUntil(()=>testComplete);
        yield break;
        void CreateMember(Pokemon createdPokemon)
        {
            createdPokemon.hasItem = true;//should fail
            createdPokemon.hasTrainer = true;
            createdPokemon.currentLevel = 5;
            AbilityHandler.CheckItemForPickUpAbility(createdPokemon);
            _currentTestPassed = createdPokemon.heldItem == null;
            
            if (!_currentTestPassed)
            {
                _testingHandler.LogMessage("Test Should fail when pokemon has held item",TestLogType.TestCase);
                testComplete = true;
                return;
            }
            
            createdPokemon.hasItem = false;
            createdPokemon.hasTrainer = false;//should fail
            createdPokemon.currentLevel = 5;
            AbilityHandler.CheckItemForPickUpAbility(createdPokemon);
            _currentTestPassed = createdPokemon.heldItem == null;
            if (!_currentTestPassed)
            {
                _testingHandler.LogMessage("Test Should fail when pokemon doesn't have a trainer",TestLogType.TestCase);
                testComplete = true;
                return;
            }
            
            createdPokemon.hasItem = false;
            createdPokemon.hasTrainer = true;
            createdPokemon.currentLevel = 4;//should fail
            AbilityHandler.CheckItemForPickUpAbility(createdPokemon);
            _currentTestPassed = createdPokemon.heldItem == null;
            if (!_currentTestPassed)
            {
                _testingHandler.LogMessage("Test Should fail when pokemon has level below 5",TestLogType.TestCase);
                testComplete = true;
                return;
            }
            
            //should pass and pokemon should have item
            createdPokemon.hasItem = false;
            createdPokemon.hasTrainer = true;
            createdPokemon.currentLevel = 5;
            AbilityHandler.CheckItemForPickUpAbility(createdPokemon);
            _currentTestPassed = createdPokemon.hasItem;
            if (!_currentTestPassed)
            {
                _testingHandler.LogMessage("Test Should fail when pokemon doesnt receive a held item",TestLogType.TestCase);
            }
            _testingHandler.LogMessage($"pokemon received a {createdPokemon.heldItem.itemName}",
                TestLogType.Information);
            testComplete = true;
        }
    }
}
