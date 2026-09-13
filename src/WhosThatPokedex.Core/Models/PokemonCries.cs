using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

public sealed record PokemonCries(
    [property: JsonPropertyName("latest")] string? Latest,
    [property: JsonPropertyName("legacy")] string? Legacy
);