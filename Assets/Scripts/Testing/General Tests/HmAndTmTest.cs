using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HmAndTmTest: ItemEndToEndTest
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private GameUiHandler _gameUiHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var itemData = (ItemEndToEndTestData)testData;
        LoadItems(itemData.testItems);
        
        //the pokemon in the data only has the move [brick break]
        
        AddTestCase("Marshtomp must not be able to learn [toxic]",() => 
            _pokemonPartyHandler.Party[0].moveSet.Count == 1);
        
        AddTestCase("Marshtomp must learn [ice beam]",() => 
            NameDB.NameMatch(_pokemonPartyHandler.Party[0].moveSet[1],MoveName.IceBeam));
        
        AddTestCase("Marshtomp must reject learning a move it already knows [ice beam]",() => 
           _pokemonPartyHandler.Party[0].moveSet.Count == 2);
        
        AddTestCase("Marshtomp must learn [rain dance]",() => 
            NameDB.NameMatch(_pokemonPartyHandler.Party[0].moveSet[2],MoveName.RainDance));
        
        AddTestCase("Marshtomp must learn [surf]",() => 
            NameDB.NameMatch(_pokemonPartyHandler.Party[0].moveSet[3],MoveName.Surf));
        
        AddTestCase("Try learning [earthquake] but reject it during dialogue option, (4th must still be surf)",
            () => NameDB.NameMatch(_pokemonPartyHandler.Party[0].moveSet[3],MoveName.Surf));
        
        AddTestCase("Try learning [earthquake] but reject it in the Move UI page of Pokemon Details UI, (4th must still be surf)",
            () => NameDB.NameMatch(_pokemonPartyHandler.Party[0].moveSet[3],MoveName.Surf));
        
        AddTestCase("Learn [earthquake] and forget brick break",
            () => NameDB.NameMatch(_pokemonPartyHandler.Party[0].moveSet[0],MoveName.Earthquake));
        
        _gameUiHandler.ValidateBagView();
        yield return null;
    }

    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Hm And Tm Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
    }
}
