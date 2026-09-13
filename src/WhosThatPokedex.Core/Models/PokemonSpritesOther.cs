using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

public sealed record PokemonSpritesOther(
    [property: JsonPropertyName("official-artwork")] OfficialArtworkSprites OfficialArtwork
);
