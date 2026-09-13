using System.Net.Http.Json;
using WhosThatPokedex.Core.Models;

namespace WhosThatPokedex.Core;

// our client for hitting the PokeAPI (primary constructor syntax)
public sealed class PokeApiClient(HttpClient httpClient)
{
    private readonly SemaphoreSlim _semaphore = new(10); // limit to 10 concurrent requests

    private async Task<GenerationResponse> GetGenerationAsync(int generationId, CancellationToken cancellationToken = default)
    {
        var generation = await httpClient.GetFromJsonAsync<GenerationResponse>($"generation/{generationId}", cancellationToken);

        // the above returns Task<T?>, so we need to check if the result is null and throw an exception if it is
        return generation ?? throw new InvalidOperationException($"No generation data returned for generation with ID `{generationId}`");
    }

    private async Task<PokemonResponse> GetPokemonAsync(string pokemonName, CancellationToken cancellationToken = default)
    {
        var pokemon = await httpClient.GetFromJsonAsync<PokemonResponse>($"pokemon/{pokemonName}", cancellationToken);
        
        // the above returns Task<T?>, so we need to check if the result is null and throw an exception if it is
        return pokemon ?? throw new InvalidOperationException($"No pokemon data returned for pokemon with name `{pokemonName}`");
    }

    // Our primary helper here is using SemaphoreSlim and try/catch/finally for the sempahore as well as error handling. We want to limit number of active requests and be resilient to errors, so we return a PokemonFetchOutcome which contains the result or the error message
    private async Task<PokemonFetchOutcome> GetPokemonFetchOutcomeAsync(string pokemonName, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var pokemon = await GetPokemonAsync(pokemonName, cancellationToken);
            return new PokemonFetchOutcome(pokemonName, pokemon, null);
        }
        catch (OperationCanceledException)
        {
            // dont swallow the cancellation into a "failure" outcome
            throw;
        }
        catch (Exception ex)
        {
            return new PokemonFetchOutcome(pokemonName, null, ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // the main entry point (for now) which will be called from the console app, to get all the pokemon for a given generation
    public async Task<GenerationFetchResult> GetPokemonForGenerationAsync(
        int generationId, 
        IProgress<PokemonFetchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // awaiting instead of using Result, because that would block synchronously
        var generation = await GetGenerationAsync(generationId, cancellationToken);

        var outcomeTasks = generation.PokemonSpecies.Select(species => GetPokemonFetchOutcomeAsync(species.Name, cancellationToken)).ToArray();

        var pokemon = new List<PokemonResponse>();
        var failures = new List<PokemonFetchOutcome>();
        var completedCount = 0;

        await foreach (var completedTask in Task.WhenEach(outcomeTasks))
        {
            var outcome = await completedTask; // this is already completed, we're just unwrapping
            completedCount++;

            if (outcome.Succeeded)
            {
                pokemon.Add(outcome.Pokemon!); // TODO: maybe I'll split this into two types so we don't need the bang operator here, but for now we know that if Succeeded is true, Pokemon is not null
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