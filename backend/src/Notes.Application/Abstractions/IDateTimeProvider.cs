namespace Notes.Application.Abstractions;

/// <summary>
/// Wraps the system clock so time-dependent rules (CreatedAt / UpdatedAt) can be
/// asserted in tests without sleeping or comparing against "roughly now".
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
