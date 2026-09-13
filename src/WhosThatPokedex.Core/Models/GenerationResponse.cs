using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

public sealed record GenerationResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("pokemon_species")] IReadOnlyList<NamedApiResource> PokemonSpecies
);