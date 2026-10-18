using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitTestHandler
{
    public enum UnitTestName{MagnitudeDamageTest,PickupAbilityItemPoolTest
        ,AmuletCoinTest,RareCandyEvolution,PokemonFriendshipModifier}

    public Dictionary<UnitTestName, Func<IEnumerator>> unitTests = new();

    private MoveLogicDatabase _moveLogicDatabase;
    private PokemonOperations _pokemonOperationsHandler;
    private TestingEnvironmentHandler _testingHandler;
    private BattleHandler _battleHandler;
    private ItemHandler _itemHandler;
    private ServiceContainer _container;
    
    private bool _currentTestPassed;
    public UnitTestHandler(ServiceContainer container)
    {
        _container = container;
        _moveLogicDatabase = container.Resolve<MoveLogicDatabase>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        _testingHandler = container.Resolve<TestingEnvironmentHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        _itemHandler = container.Resolve<ItemHandler>();
      
        unitTests.Add(UnitTestName.MagnitudeDamageTest,MagnitudeDamageTest);
        unitTests.Add(UnitTestName.PickupAbilityItemPoolTest,PickupAbilityItemPoolTest);
        unitTests.Add(UnitTestName.AmuletCoinTest,AmuletCoinTest);
        unitTests.Add(UnitTestName.RareCandyEvolution,RareCandyEvolutionTest);
        unitTests.Add(UnitTestName.PokemonFriendshipModifier,PokemonFriendshipModifierTest);
    }

    public IEnumerator RunTests()
    {
        foreach (var test in unitTests)
        {
            _currentTestPassed = false;
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
            _currentTestPassed = createdPokemon.heldItem is null;
            
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
            _currentTestPassed = createdPokemon.heldItem is null;
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
            _currentTestPassed = createdPokemon.heldItem is null;
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
            _testingHandler.LogMessage($"pokemon received a {createdPokemon.heldItem?.itemName}",
                TestLogType.Information);
            testComplete = true;
        }
    }

    private IEnumerator AmuletCoinTest()
    {
        bool testComplete = false;
        
        var participants = new List<BattleParticipant>
        {
            new BattleParticipant{ isActive = true,isPlayer = true},
            new BattleParticipant{ isActive = true,isPlayer = false},
            new BattleParticipant{ isActive = true,isPlayer = true},
            new BattleParticipant{ isActive = false,isPlayer = false},
        }; 
      
        var pokemonAsset = Resources.Load<Pokemon>(DirectoryHandler.GetDirectory(AssetDirectory.Pokemon) + "Zigzagoon/Zigzagoon");
        var amuletCoin = Resources.Load<Item>(DirectoryHandler.GetDirectory(AssetDirectory.Items)
        + NameDB.GetItem(ItemName.AmuletCoin));
        yield return _pokemonOperationsHandler.HandlePokemonCreation(CreateMember,pokemonAsset,5,1);
        
        yield return new WaitUntil(()=>testComplete);
        yield break;
        void CreateMember(Pokemon createdPokemon)
        {
            createdPokemon.GiveItem(amuletCoin);
            participants[0].pokemonState = new PokemonStateHandler(_container,createdPokemon,false);
            participants[0].ReceivePokemon(createdPokemon);
            var moneyBonus = _battleHandler.PrizeMoneyModifier(participants);
            _testingHandler.LogMessage("Test Requires money bonus greater than 1",TestLogType.TestCase);
            _testingHandler.LogMessage($"Money bonus: {moneyBonus}",TestLogType.Calculation);
            _currentTestPassed = moneyBonus > 1f;
            testComplete = true;
        }
    }

    private IEnumerator RareCandyEvolutionTest()
    {
        bool testComplete = false;
       
        var pokemonAsset = Resources.Load<Pokemon>(DirectoryHandler.GetDirectory(AssetDirectory.Pokemon) + "Zubat/Zubat");
       
        var rareCandy = Resources.Load<Item>(DirectoryHandler.GetDirectory(AssetDirectory.Items)
                                              + NameDB.GetItem(ItemName.RareCandy));
       
        yield return _pokemonOperationsHandler.HandlePokemonCreation(CreateMember,pokemonAsset,21,0);
        yield return new WaitUntil(()=>testComplete);
        yield break;
        void CreateMember(Pokemon createdPokemon)
        {
            _testingHandler.LogMessage($"Using rare candy on {createdPokemon.pokemonName}",TestLogType.Information);
            var testPokemonState = new PokemonStateHandler(_container,createdPokemon,false);
            _itemHandler.OnItemUsed += CheckEvolution;
            _itemHandler.UseItem(rareCandy,testPokemonState);
            return;
            void CheckEvolution(Item item,bool successful)
            {
                if (item.itemName == rareCandy.itemName)
                {
                    _itemHandler.OnItemUsed -= CheckEvolution;
                    _testingHandler.LogMessage("Evolution Succeeded",TestLogType.Information);
                    _currentTestPassed =  testPokemonState.pokemon.currentLevel == 22
                                          &&  testPokemonState.pokemon.pokemonName == "Golbat";
                    testComplete = true;
                }
            }
        }
    }
    private IEnumerator PokemonFriendshipModifierTest()
    {
        bool testComplete = false;
        var pokemonAsset = Resources.Load<Pokemon>(DirectoryHandler.GetDirectory(AssetDirectory.Pokemon) + "Zubat/Zubat");
        
        var sootheBell = Resources.Load<Item>(DirectoryHandler.GetDirectory(AssetDirectory.Items)
                                              + NameDB.GetItem(ItemName.SootheBell));
        
        yield return _pokemonOperationsHandler.HandlePokemonCreation(CreateMember,pokemonAsset,21,0);
        yield return new WaitUntil(()=>testComplete);
        yield break;
        void CreateMember(Pokemon createdPokemon)
        {
            createdPokemon.GiveItem(sootheBell);
            createdPokemon.pokeballName = NameDB.GetItem(ItemName.LuxuryBall);
            createdPokemon.friendshipLevel = 0;
            
            createdPokemon.DetermineFriendshipLevelChange(FriendshipModifier.Berry);
            createdPokemon.DetermineFriendshipLevelChange(FriendshipModifier.XItem);
            
            _testingHandler.LogMessage($"Friendship increase from 0 to {createdPokemon.friendshipLevel}",TestLogType.Calculation);
            //X Item = 1 , mods * 4
            //berry = 10, mods *4 
            //total = 44
            _currentTestPassed =  createdPokemon.friendshipLevel == 44;
            testComplete = true;
        }
    }
}
