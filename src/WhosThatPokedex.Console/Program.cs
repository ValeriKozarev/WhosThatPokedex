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

// create a cancellation token source that will listen to Ctrl+C and cancel the operation
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true; // prevent the process from being killed outright
    cts.Cancel();     // trigger our own graceful cancellation instead
    Console.WriteLine("Cancellation requested...");
};

// testing to see if we handled cache stampede -- this outputs progress only once, the two batches are not interleaved because one is reading from the cache
// var stampedeTask1 = pokeApiClient.GetPokemonForGenerationAsync(2, progress, cts.Token);
// var stampedeTask2 = pokeApiClient.GetPokemonForGenerationAsync(2, progress, cts.Token);
// await Task.WhenAll(stampedeTask1, stampedeTask2);

try
{
    var result = await pokeApiClient.GetPokemonForGenerationAsync(1, progress, cts.Token);
    Console.WriteLine($"Pokemon species in this generation: {result.Pokemon.Count}");
    Console.WriteLine($"Pokemon species that failed to fetch: {result.Failures.Count}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was canceled.");
}
catch (Exception ex)
{
    Console.WriteLine($"Couldn't fetch generation data: {ex.Message}");
}
