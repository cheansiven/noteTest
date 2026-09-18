using Notes.Application.Abstractions;

namespace Notes.Application.UnitTests.Doubles;

/// <summary>A clock the test drives, so timestamp rules can be asserted exactly.</summary>
public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTime? start = null) =>
        UtcNow = start ?? new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);

    public DateTime UtcNow { get; private set; }

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
