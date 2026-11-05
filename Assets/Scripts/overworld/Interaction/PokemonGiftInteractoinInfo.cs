using System;

[Serializable]
public class PokemonGiftInteractoinInfo : DynamicAdditionalInfo
{
    public NaturalPokemonCreationData pokemonData;
    
    public override void HandleInternalLogic(ServiceContainer container)
    {
        var pokemonStorage = container.Resolve<PokemonStorageHandler>();
        var playerParty = container.Resolve<PokemonPartyHandler>();
        var dialogueHandler =  container.Resolve<DialogueHandler>();
        
        if(pokemonStorage.MaxPokemonCapacity())
        {
            dialogueHandler.DisplayDetails("Can no longer obtain more pokemon, free up space in pc!");
            return;
        }
        playerParty.AddGiftMember(this);
    }
}
