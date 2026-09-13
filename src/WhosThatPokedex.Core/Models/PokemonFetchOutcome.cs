using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

// this isn't shaped by the API so its not a DTO, its a domain type that we've made to fit our needs of handling API outcomes, so we can return a single object that contains either the result or the error message
public sealed record PokemonFetchOutcome(string PokemonName, PokemonResponse? Pokemon, string? ErrorMessage)
{
    public bool Succeeded => Pokemon is not null;
}