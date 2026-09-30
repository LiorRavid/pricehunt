using System.Diagnostics.CodeAnalysis;

namespace PriceHunt.Application.Searches;

/// <summary>Either a runnable plan or the validation errors, keyed by request field.</summary>
public sealed class SearchPlanResult
{
    private SearchPlanResult(SearchPlan? plan, IReadOnlyDictionary<string, string[]> errors)
    {
        Plan = plan;
        Errors = errors;
    }

    /// <summary>Gets the plan, when the request is valid.</summary>
    public SearchPlan? Plan { get; }

    /// <summary>Gets the validation errors by field (see <see cref="SearchRequestFields"/>); empty when valid.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>Gets a value indicating whether the request is valid.</summary>
    [MemberNotNullWhen(true, nameof(Plan))]
    public bool IsValid => Plan is not null;

    internal static SearchPlanResult Valid(SearchPlan plan) => new(plan, new Dictionary<string, string[]>());

    internal static SearchPlanResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(null, errors);
}
