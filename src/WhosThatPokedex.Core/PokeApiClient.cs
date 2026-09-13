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

    // Our primary helper here is using SemaphoreSlim and try/catch/finally for the sempahore as well as error handling. We want to limit number of active requests and be resilient to errors, so we return a PokemonFetchOutcome which contains the result or the error message
    private async Task<PokemonFetchOutcome> GetPokemonFetchOutcomeAsync(string pokemonName)
    {
        await semaphore.WaitAsync();
        try
        {
            var pokemon = await GetPokemonAsync(pokemonName);
            return new PokemonFetchOutcome(pokemonName, pokemon, null);
        }
        catch (Exception ex)
        {
            return new PokemonFetchOutcome(pokemonName, null, ex.Message);
        }
        finally
        {
            semaphore.Release();
        }
    }

    // the main entry point (for now) which will be called from the console app, to get all the pokemon for a given generation
    public async Task<GenerationFetchResult> GetPokemonForGenerationAsync(int generationId, IProgress<PokemonFetchProgress>? progress = null)
    {
        // awaiting instead of using Result, because that would block synchronously
        var generation = await GetGenerationAsync(generationId);

        var outcomeTasks = generation.PokemonSpecies.Select(species => GetPokemonFetchOutcomeAsync(species.Name)).ToArray();

        var pokemon = new List<PokemonResponse>();
        var failures = new List<PokemonFetchOutcome>();
        var completedCount = 0;

        await foreach (var completedTask in Task.WhenEach(outcomeTasks))
        {
            var outcome = await completedTask; // this is already completed, we're just unwrapping
            completedCount++;

            if (outcome.Succeeded)
            {
                pokemon.Add(outcome.Pokemon!);
            }
            else
            {
                failures.Add(outcome);
            }

            progress?.Report(new PokemonFetchProgress(generation.PokemonSpecies.Count, pokemon.Count, failures.Count));
        }

        return new GenerationFetchResult(pokemon, failures);
    }
}