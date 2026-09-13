namespace WhosThatPokedex.Core.Models;

// our own classification of "where is this Pokemon in its evolution line" — not
// something the API hands us directly, we derive it by walking the evolution chain
public enum EvolutionStage
{
    DoesNotEvolve,
    BaseWithMultipleEvolutionsAhead,
    BaseWithOneEvolutionAhead,
    MidEvolutionCanEvolveFurther,
    FullyEvolved
}
