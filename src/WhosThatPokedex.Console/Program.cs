using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;

using WhosThatPokedex.Core;

// Creating a Generic Host which is responsible for managing the application lifecycle and dependency injection.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Register our client for hitting the PokeAPI so we can use it later
builder.Services.AddHttpClient<PokeApiClient>(client =>
{
   client.BaseAddress = new Uri("https://pokeapi.co/api/v2/"); 
});

// end of registration phase
using IHost host = builder.Build();

var pokeApiClient = host.Services.GetRequiredService<PokeApiClient>();

Stopwatch stopwatch3 = Stopwatch.StartNew();
var pokemonList = await pokeApiClient.GetPokemonForGenerationAsync(1);
stopwatch3.Stop();
TimeSpan ts3 = stopwatch3.Elapsed;
Console.WriteLine($"Pokemon species in this generation: {pokemonList.Count}");
Console.WriteLine($"Time taken to fetch all pokemon data for generation: {ts3.TotalMilliseconds} ms");
// NOTE: running this with a WhenAll sent out 151 requests at once, and took 1231.23ms
// NOTE: running this with a SemaphoreSlim to limit the number of concurrent requests to 10, took 960.76ms (network conditions vary, point is it works)