using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

// recursive shape matching the API's own recursive "chain" tree — each link knows its
// own species and the list of links it can evolve into (empty if it's a final form)
public sealed record EvolutionChainLink(
    [property: JsonPropertyName("species")] NamedApiResource Species,
    [property: JsonPropertyName("evolves_to")] IReadOnlyList<EvolutionChainLink> EvolvesTo
);
