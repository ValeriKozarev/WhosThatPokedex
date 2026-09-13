# WhosThatPokedex

A small .NET project for practicing the patterns from *Concurrency in C# Cookbook*
(Stephen Cleary, 2nd ed.) by building a pokedex client against [PokéAPI](https://pokeapi.co/).

## Structure

- `src/WhosThatPokedex.Core` — `PokeApiClient` and all concurrency/fetching logic. No UI dependencies.
- `src/WhosThatPokedex.Console` — thin console harness that drives `PokeApiClient`.
- `tests/WhosThatPokedex.Core.Tests` — xUnit test project (currently just the scaffold stub).

## What's built so far

`PokeApiClient.GetPokemonForGenerationAsync(generationId, progress, cancellationToken)` fetches a
generation's species list, then fans out to fetch every Pokémon in it concurrently. Along the way it
applies:

- **`async`/`await`** fundamentals, `IHttpClientFactory` via a typed client
- **`Task.WhenEach`** to process each Pokémon as its fetch completes, instead of waiting on the whole
  batch (`Task.WhenAll`) and losing everything if one request fails
- **`SemaphoreSlim`** to cap concurrent requests (max 10) instead of firing all ~150 at once
- **Per-item error handling** (`PokemonFetchOutcome`) — one failed Pokémon doesn't take down the rest;
  results come back as successes + a list of failures, not all-or-nothing
- **`IProgress<T>`** to report fetch progress back to the caller
- **`CancellationToken`**, threaded all the way down to the HTTP calls and the semaphore wait, wired up
  to Ctrl+C in the console app
- **Cache-stampede-safe caching** of generation results (`ConcurrentDictionary<int, Lazy<Task<T>>>`),
  with eviction on failure so a transient error doesn't get permanently cached

## Running it

```bash
dotnet run --project src/WhosThatPokedex.Console
```

## Testing

```bash
dotnet test
```

## TODO

- [ ] Write real unit tests for `PokeApiClient` (throttling, per-item failure handling, progress
      reporting, cancellation, caching) — currently untested beyond manual runs
- [ ] Design and build a real UI — still just a console harness today
- [ ] Eventually: turn this into a "who's that Pokémon?" guessing game
