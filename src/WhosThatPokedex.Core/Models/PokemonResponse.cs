using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

public sealed record PokemonResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("weight")] int Weight,
    [property: JsonPropertyName("types")] IReadOnlyList<PokemonType> Types,
    [property: JsonPropertyName("sprites")] PokemonSprites Sprites
);