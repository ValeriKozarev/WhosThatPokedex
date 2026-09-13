using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

public sealed record PokemonSprites(
    [property: JsonPropertyName("front_default")] string? FrontDefault,
    [property: JsonPropertyName("other")] PokemonSpritesOther Other
);