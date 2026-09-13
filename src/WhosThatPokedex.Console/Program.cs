using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
var generation = await pokeApiClient.GetGenerationAsync(1);

Console.WriteLine($"Generation {generation.Id}: {generation.Name}");
Console.WriteLine($"Pokemon species in this generation: {generation.PokemonSpecies.Count}");

var pokemon = await pokeApiClient.GetPokemonAsync("bulbasaur");
Console.WriteLine($"Pokemon: {pokemon.Name}");
Console.WriteLine($"Official artwork: {pokemon.Sprites.Other.OfficialArtwork.FrontDefault}");