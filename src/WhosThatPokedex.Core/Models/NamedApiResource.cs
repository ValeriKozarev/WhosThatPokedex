using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

// this is the standard shape that the PokeAPI uses, so we'll use it here to make response typing simpler
public sealed record NamedApiResource(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url
);