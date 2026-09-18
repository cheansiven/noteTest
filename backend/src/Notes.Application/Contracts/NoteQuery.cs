namespace Notes.Application.Contracts;

public enum NoteSortField
{
    UpdatedAt = 0,
    CreatedAt = 1,
    Title = 2,
}

public enum SortDirection
{
    Desc = 0,
    Asc = 1,
}

/// <summary>Search / filter / sort / paging options for GET /api/notes.</summary>
public sealed record NoteQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Free text, matched against title and content.</summary>
    public string? Search { get; init; }

    public NoteSortField SortBy { get; init; } = NoteSortField.UpdatedAt;

    public SortDirection SortDirection { get; init; } = SortDirection.Desc;

    /// <summary>Inclusive lower bound on CreatedAt (UTC).</summary>
    public DateTime? CreatedFrom { get; init; }

    /// <summary>Inclusive upper bound on CreatedAt (UTC).</summary>
    public DateTime? CreatedTo { get; init; }

    /// <summary>When set, keeps only notes that do (or do not) have body content.</summary>
    public bool? HasContent { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>
    /// Clamps client input into a supported range and stretches a date-only upper bound
    /// to the end of that day, so "created to 18 Sep" includes notes written that evening.
    /// </summary>
    public NoteQuery Normalized() => this with
    {
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        Page = Page < 1 ? 1 : Page,
        PageSize = PageSize is < 1 or > MaxPageSize ? DefaultPageSize : PageSize,
        CreatedTo = CreatedTo is { } to && to.TimeOfDay == TimeSpan.Zero
            ? to.AddDays(1).AddTicks(-1)
            : CreatedTo,
    };
}
