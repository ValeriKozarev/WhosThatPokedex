using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

public sealed record EvolutionChainResponse(
    [property: JsonPropertyName("chain")] EvolutionChainLink Chain
);
