using System.Text.Json.Serialization;

namespace WhosThatPokedex.Core.Models;

// another type just for us, we'll use this to report progress back to the console app, so we can see how many pokemon have been fetched and how many have failed as we wait

public sealed record PokemonFetchProgress(
    int TotalPokemon,
    int FetchedPokemon,
    int FailedPokemon
);