using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RareCandyTest : EndToEndTest,IItemTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private GameUiHandler _gameUiHandler;
    
    private int expectedLevel;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var itemData = (ItemEndToEndTestData)testData;
        this.LoadItems(itemData.testItems);
        
        //the test data has a pokemon of level 5, with 1 move [Ariel ace]
        AddTestCaseScenario(()=>
        {
            expectedLevel = _pokemonPartyHandler.Party[0].currentLevel+1;
        });
        AddTestCaseScenario(()=>
        {
            expectedLevel++;
            //fill move cap and force move replacement
            //when level up again
            var moveName = NameDB.GetMoveName(MoveName.LeafBlade);
            var assetPath = DirectoryHandler.GetDirectory(AssetDirectory.Moves) + moveName;
            var moveFromAsset = Resources.Load<Move>(assetPath);
            var newMove = InstanceFactory.CreateMove(moveFromAsset);
            _pokemonPartyHandler.Party[0].moveSet.Add(newMove);
            _pokemonPartyHandler.Party[0].moveSet.Add(newMove);
            
            //allow rare candy to be the trigger for learning water gun
            _pokemonPartyHandler.Party[0].learnSet[3].requiredLevel = 7;
        });
        
        AddTestCase(new List<TestCaseCondition>
        { 
            new("Pokemon must level up by 1",
                () => _pokemonPartyHandler.Party[0].currentLevel == expectedLevel), 
            new("Must learn mud slap",
                () => NameDB.NameMatch(_pokemonPartyHandler.Party[0].moveSet[1],MoveName.MudSlap))   
        });
        
        AddTestCase(new List<TestCaseCondition>
        { 
            new("Pokemon must level up by 1",
                () => _pokemonPartyHandler.Party[0].currentLevel == expectedLevel), 
            new("Must forget leaf blade[4th move] and learn water gun",
                () => NameDB.NameMatch(_pokemonPartyHandler.Party[0].moveSet[3],MoveName.WaterGun))   
        });
        
        _gameUiHandler.ValidateBagView();
        yield return null;
    }

    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Rare Candy Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
    }
}
