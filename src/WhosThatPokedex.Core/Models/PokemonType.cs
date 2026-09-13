using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

public sealed record PokemonType(
    [property: JsonPropertyName("slot")] int Slot,
    [property: JsonPropertyName("type")] NamedApiResource Type
);