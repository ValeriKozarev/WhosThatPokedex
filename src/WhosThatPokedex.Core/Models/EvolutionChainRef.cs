using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

// unlike most references in this API, this one is just a bare URL with no "name" —
// not every reference is a NamedApiResource, so we model this one separately rather
// than forcing it into that shape
public sealed record EvolutionChainRef(
    [property: JsonPropertyName("url")] string Url
);
