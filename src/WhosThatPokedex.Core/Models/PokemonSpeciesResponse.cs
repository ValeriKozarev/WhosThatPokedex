using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

// the species endpoint has a lot more on it than this, but the evolution chain link
// is the only thing we currently need from it
public sealed record PokemonSpeciesResponse(
    [property: JsonPropertyName("evolution_chain")] EvolutionChainRef EvolutionChain
);
