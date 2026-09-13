using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

public sealed record OfficialArtworkSprites(
    [property: JsonPropertyName("front_default")] string? FrontDefault
);