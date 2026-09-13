# WhosThatPokedex

A "Who's That Pokémon?" guessing game, and a practice project for the patterns from
*Concurrency in C# Cookbook* (Stephen Cleary, 2nd ed.), built against [PokéAPI](https://pokeapi.co/).

## Structure

- `src/WhosThatPokedex.Core` — `PokeApiClient` and all concurrency/fetching logic. No UI dependencies.
- `src/WhosThatPokedex.Web` — the actual game, a Blazor Server app.
- `src/WhosThatPokedex.Console` — thin console harness used while building out `PokeApiClient`; not the game.
- `tests/WhosThatPokedex.Core.Tests` — xUnit test project (currently just the scaffold stub).

## The game

Pick one or more generations, get shown a silhouette of a random Pokémon from the combined pool, and
guess who it is in 3 tries.

- Multi-select generation picker (at least one required); selected generations are fetched **concurrently**
  and merged into one pool
- Silhouette via a CSS `brightness(0)` filter on the official artwork — revealed on a correct guess or
  after 3 misses
- Sorted, grouped dropdown for guessing — sectioned by generation (`<optgroup>`), alphabetical within each
- Guesses and hints each shown as a row of 3 dots that grey out as they're used (shared `TierDots` component)
- 3-tier hint system: region/generation, evolution stage (base/mid-evolution/fully evolved — derived by
  walking PokéAPI's evolution-chain tree), then type(s)
- "Hear its call" — plays the Pokémon's cry via a small JS interop call, with a cancellation-safe 5-second
  cooldown (a stale cooldown timer can't clear a newer one if you start a new game mid-cooldown)

## Concurrency patterns in `PokeApiClient`

`GetPokemonForGenerationAsync(generationId, progress, cancellationToken)` fetches a generation's species
list, then fans out to fetch every Pokémon in it concurrently. Along the way it applies:

- **`async`/`await`** fundamentals, `IHttpClientFactory` via a typed client
- **`Task.WhenEach`** to process each Pokémon as its fetch completes, instead of waiting on the whole
  batch (`Task.WhenAll`) and losing everything if one request fails
- **`SemaphoreSlim`** to cap concurrent requests (max 10) instead of firing all ~150 at once
- **Per-item error handling** (`PokemonFetchOutcome`) — one failed Pokémon doesn't take down the rest;
  results come back as successes + a list of failures, not all-or-nothing
- **`IProgress<T>`** to report fetch progress back to the caller
- **`CancellationToken`**, threaded all the way down to the HTTP calls and the semaphore wait
- **Cache-stampede-safe caching** of generation results (`ConcurrentDictionary<int, Lazy<Task<T>>>`),
  with eviction on failure so a transient error doesn't get permanently cached

## Running it

```bash
dotnet run --project src/WhosThatPokedex.Web
```

Opens a local URL — the console harness (`dotnet run --project src/WhosThatPokedex.Console`) still works
too, but just exercises `PokeApiClient` directly with no game UI.

## Testing

```bash
dotnet test
```

## TODO

- [ ] Write real unit tests for `PokeApiClient` (throttling, per-item failure handling, progress
      reporting, cancellation, caching) — currently untested beyond manual runs
- [ ] UI polish/styling pass — functional but plain right now
