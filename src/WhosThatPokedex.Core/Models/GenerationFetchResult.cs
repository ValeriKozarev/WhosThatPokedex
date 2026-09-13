namespace WhosThatPokedex.Core.Models;

// another custom type we've made to fit our needs, this one is for returning the result of fetching all the pokemon for a given generation, including any failures
public sealed record GenerationFetchResult(
    IReadOnlyList<PokemonResponse> Pokemon,
    IReadOnlyList<PokemonFetchOutcome> Failures
);