using System.Collections;
using System.Collections.Generic;

public class EvolutionInBattleTest: EndToEndTest,IBattleTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private BattleHandler _battleHandler;
    private string previousPokemonName;
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        _battleHandler.OnBattleStarted += SetupHp;
        void SetupHp()
        { 
            _battleHandler.OnBattleStarted -= SetupHp;
            var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
            enemy.pokemon.hp = 1;
        }
        
        //This test is using a level 15 mudkip
        AddTestCaseScenario(()=>
        {
            previousPokemonName = _pokemonPartyHandler.Party[0].pokemonName;
            //guarantee level up after any exp gain
            _pokemonPartyHandler.Party[0].currentExpAmount = _pokemonPartyHandler.Party[0].nextLevelExpAmount - 1;
            //guarantee enemy will faint
            _pokemonPartyHandler.Party[0].moveSet[0].isSureHit = true;
            _pokemonPartyHandler.Party[0].moveSet[0].moveDamage = 200f;
        });
        //Run test case after battle
        AddTestCase(new List<TestCaseCondition>{
            new("Player must level up",
                () => _pokemonPartyHandler.Party[0].currentLevel==16),
            new("Pokemon must evolve from Mudkip to Marshtomp",
                ()=> _pokemonPartyHandler.Party[0].pokemonName!=previousPokemonName)
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
        testName = "Evolution In Battle Test";
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
    }
}