using System.Net.Http.Json;
using WhosThatPokedex.Core.Models;

namespace WhosThatPokedex.Core;

// our client for hitting the PokeAPI so we can use it later with primary constructor syntax
public sealed class PokeApiClient(HttpClient httpClient)
{
    private readonly SemaphoreSlim semaphore = new(10); // limit to 10 concurrent requests

    private async Task<GenerationResponse> GetGenerationAsync(int generationId)
    {
        var generation = await httpClient.GetFromJsonAsync<GenerationResponse>($"generation/{generationId}");
        
        // the above returns Task<T?>, so we need to check if the result is null and throw an exception if it is
        return generation ?? throw new InvalidOperationException($"No generation data returned for generation with ID `{generationId}`");
    }

    private async Task<PokemonResponse> GetPokemonAsync(string pokemonName)
    {
        var pokemon = await httpClient.GetFromJsonAsync<PokemonResponse>($"pokemon/{pokemonName}");
        
        // the above returns Task<T?>, so we need to check if the result is null and throw an exception if it is
        return pokemon ?? throw new InvalidOperationException($"No pokemon data returned for pokemon with name `{pokemonName}`");
    }

    // Our helper using SemaphoreSlim to limit the number of concurrent requests to the PokeAPI
    private async Task<PokemonResponse> GetPokemonSemaphoreTaskAsync(string pokemonName)
    {
        await semaphore.WaitAsync();
        try
        {
            return await GetPokemonAsync(pokemonName);
        }
        finally
        {
            semaphore.Release();
        }
    }

    // the main entry point (for now) which will be called from the console app, to get all the pokemon for a given generation
    public async Task<IReadOnlyList<PokemonResponse>> GetPokemonForGenerationAsync(int generationId)
    {
        // awaiting instead of using Result, because that would block synchronously
        var generation = await GetGenerationAsync(generationId);

        // we need .ToArray() because LINQ is lazy and won't start until enumeration
        var pokemonTasks = generation.PokemonSpecies.Select(species => GetPokemonSemaphoreTaskAsync(species.Name)).ToArray();
        return await Task.WhenAll(pokemonTasks);
    }
}