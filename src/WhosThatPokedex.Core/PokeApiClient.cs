using System.Net.Http.Json;
using WhosThatPokedex.Core.Models;

namespace WhosThatPokedex.Core;

// our client for hitting the PokeAPI so we can use it later with primary constructor syntax
public sealed class PokeApiClient(HttpClient httpClient)
{
    public async Task<GenerationResponse> GetGenerationAsync(int generationId)
    {
        var generation = await httpClient.GetFromJsonAsync<GenerationResponse>($"generation/{generationId}");
        
        // the above returns Task<T?>, so we need to check if the result is null and throw an exception if it is
        return generation ?? throw new InvalidOperationException($"No generation data returned for generation with ID `{generationId}`");
    }
}