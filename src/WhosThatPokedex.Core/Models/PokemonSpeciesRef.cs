using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

public sealed record PokemonSpeciesRef(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url
);