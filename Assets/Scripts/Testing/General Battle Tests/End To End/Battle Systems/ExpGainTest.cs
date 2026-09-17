using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpGainTest: EndToEndTest,IBattleTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private BattleHandler _battleHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        //This test is using a level 11 mudkip
        AddTestCaseScenario(()=>
        {
            //guarantee level up after any exp gain
            _pokemonPartyHandler.Party[0].currentExpAmount = _pokemonPartyHandler.Party[0].nextLevelExpAmount - 1;
            //guarantee enemy will be hit
            _pokemonPartyHandler.Party[0].moveSet[0].isSureHit = true;
            _pokemonPartyHandler.Party[0].moveSet[0].moveDamage = 200f;
            _pokemonPartyHandler.Party[0].learnSet[4].requiredLevel = _pokemonPartyHandler.Party[0].currentLevel + 1;
        });
        AddTestCaseScenario(()=>
        {
            //guarantee level up after any exp gain
            _pokemonPartyHandler.Party[0].currentExpAmount = _pokemonPartyHandler.Party[0].nextLevelExpAmount - 1;
            //guarantee enemy will be hit
            _pokemonPartyHandler.Party[0].moveSet[0].isSureHit = true;
            enemy.pokemon.hp = 1;
            enemy.pokemon.expYield *= 2;//calculated this using previous run of this test
            //guarantee move learn
            _pokemonPartyHandler.Party[0].learnSet[5].requiredLevel = _pokemonPartyHandler.Party[0].currentLevel + 1;
            //guarantee next move learn
            _pokemonPartyHandler.Party[0].learnSet[6].requiredLevel = _pokemonPartyHandler.Party[0].currentLevel + 2;
        });
        
        AddTestCase(new List<TestCaseCondition>{
            new("Player must faint weak enemy and level",
                () => _pokemonPartyHandler.Party[0].currentLevel==12),
            new("Player must learn 1 moves from learnset",
                ()=> _pokemonPartyHandler.Party[0].moveSet.Count==3)
        });
        
        AddTestCase(new List<TestCaseCondition>{
            new("Player must learn a fourth move from learnset",
                ()=> _pokemonPartyHandler.Party[0].moveSet.Count==4),
            new("Player must replace first move with MudSport",
                ()=> NameDB.NameMatch(_pokemonPartyHandler.Party[0].moveSet[0],MoveName.MudSport))
        });
        
        this.StartBattle(battleTestData,this);
        yield return null;
    }
    protected override void EndTest()
    { 
        //this test makes the player beat the enemy 
        //so no need to manually end battle
    }
    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Exp Gain Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
    }
}