using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using WhosThatPokedex.Core;
using WhosThatPokedex.Core.Models;

// Creating a Generic Host which is responsible for managing the application lifecycle and dependency injection.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// suppress some of the logging so we can focus on showing our progress bar
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

// Register our client for hitting the PokeAPI so we can use it later
builder.Services.AddHttpClient<PokeApiClient>(client =>
{
   client.BaseAddress = new Uri("https://pokeapi.co/api/v2/"); 
});

// end of registration phase
using IHost host = builder.Build();

var pokeApiClient = host.Services.GetRequiredService<PokeApiClient>();

// progress callback function so we can show the user what's going on
var progress = new Progress<PokemonFetchProgress>(p =>
{
    Console.WriteLine($"Progress: {p.FetchedPokemon}/{p.TotalPokemon} fetched, {p.FailedPokemon} failed");
});

var genFetchResult = await pokeApiClient.GetPokemonForGenerationAsync(1, progress);

Console.WriteLine($"Pokemon species in this generation: {genFetchResult.Pokemon.Count}");
Console.WriteLine($"Pokemon species that failed to fetch: {genFetchResult.Failures.Count}");