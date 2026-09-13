using System.Net.Http.Json;
using System.Collections.Concurrent;
using WhosThatPokedex.Core.Models;

namespace WhosThatPokedex.Core;

// our client for hitting the PokeAPI (primary constructor syntax)
public sealed class PokeApiClient(HttpClient httpClient)
{
    private readonly SemaphoreSlim _semaphore = new(10); // limit to 10 concurrent requests
    private readonly ConcurrentDictionary<int, Lazy<Task<GenerationFetchResult>>> _generationCache = new();

    #region private API helpers
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

    private async Task<GenerationFetchResult> AwaitAndEvictOnFailureAsync(int generationId, Lazy<Task<GenerationFetchResult>> lazyResult)
    {
        try
        {
            return await lazyResult.Value;
        }
        catch
        {
            // if the task failed, we want to evict it from the cache so that we can try again next time
            _generationCache.TryRemove(generationId, out _);
            throw;
        }
    }

    // this is the main driver of the app because it fetches all the pokemon for a given generation, and reports progress back to the caller along with being able to handle failures and cancellation
    // hint helper: fetches a Pokemon's evolution chain and classifies where it sits in it.
    // Note the species/evolution-chain URLs from the API are already full absolute URLs —
    // HttpClient ignores BaseAddress when given an absolute URI, so we can pass them straight through.
    public async Task<EvolutionStage> GetEvolutionStageAsync(PokemonResponse pokemon, CancellationToken cancellationToken = default)
    {
        var species = await GetPokemonSpeciesAsync(pokemon.Species.Url, cancellationToken);
        var evolutionChain = await GetEvolutionChainAsync(species.EvolutionChain.Url, cancellationToken);

        var node = FindNode(evolutionChain.Chain, pokemon.Name)
            ?? throw new InvalidOperationException($"Could not find `{pokemon.Name}` in its own evolution chain.");

        var isBaseForm = string.Equals(evolutionChain.Chain.Species.Name, pokemon.Name, StringComparison.OrdinalIgnoreCase);
        var canEvolveFurther = node.EvolvesTo.Count > 0;

        if (isBaseForm)
        {
            if (!canEvolveFurther)
                return EvolutionStage.DoesNotEvolve;

            var evolvesMultipleTimes = node.EvolvesTo.Any(child => child.EvolvesTo.Count > 0);
            return evolvesMultipleTimes
                ? EvolutionStage.BaseWithMultipleEvolutionsAhead
                : EvolutionStage.BaseWithOneEvolutionAhead;
        }

        return canEvolveFurther ? EvolutionStage.MidEvolutionCanEvolveFurther : EvolutionStage.FullyEvolved;
    }

    // recursively searches the evolution chain tree for the node matching this species name
    private static EvolutionChainLink? FindNode(EvolutionChainLink node, string speciesName)
    {
        if (string.Equals(node.Species.Name, speciesName, StringComparison.OrdinalIgnoreCase))
            return node;

        foreach (var child in node.EvolvesTo)
        {
            var found = FindNode(child, speciesName);
            if (found is not null)
                return found;
        }

        return null;
    }

    private async Task<PokemonSpeciesResponse> GetPokemonSpeciesAsync(string speciesUrl, CancellationToken cancellationToken = default)
    {
        var species = await httpClient.GetFromJsonAsync<PokemonSpeciesResponse>(speciesUrl, cancellationToken);
        return species ?? throw new InvalidOperationException($"No species data returned for `{speciesUrl}`");
    }

    private async Task<EvolutionChainResponse> GetEvolutionChainAsync(string evolutionChainUrl, CancellationToken cancellationToken = default)
    {
        var chain = await httpClient.GetFromJsonAsync<EvolutionChainResponse>(evolutionChainUrl, cancellationToken);
        return chain ?? throw new InvalidOperationException($"No evolution chain data returned for `{evolutionChainUrl}`");
    }

    private async Task<GenerationFetchResult> FetchPokemonForGenerationAsync(
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
    #endregion


    // the main entry point (for now) which will be called from the console app, to get all the pokemon for a given generation
    public Task<GenerationFetchResult> GetPokemonForGenerationAsync(
        int generationId, 
        IProgress<PokemonFetchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // this line looks crazy, but it's pretty cool. We are storing an unstarted promise in a Lazy<Task<T>> so that we can avoid starting the fetch until we actually need it, and we are using a ConcurrentDictionary to cache the result so that if multiple calls come in for the same generation, we only fetch it once. If the fetch fails, we evict it from the cache so that we can try again next time.
        var lazyResult = _generationCache.GetOrAdd(generationId, id => new Lazy<Task<GenerationFetchResult>>(() => FetchPokemonForGenerationAsync(id, progress, cancellationToken)));

        return AwaitAndEvictOnFailureAsync(generationId, lazyResult);
    }
}