using Notes.Application.Abstractions;

namespace Notes.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// The wall clock, truncated to whole milliseconds.
    /// <para>
    /// Timestamps are stored in DATETIME2(3) and serialised with millisecond precision,
    /// while <see cref="DateTime.UtcNow"/> carries 100-nanosecond ticks. Without this
    /// truncation SQL Server rounds on the way in, so the CreatedAt a client receives
    /// from a create call could differ from the value a later read returns. Holding the
    /// whole system to one resolution removes that drift.
    /// </para>
    /// </summary>
    public DateTime UtcNow
    {
        get
        {
            var now = DateTime.UtcNow;
            return new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }
    }
}
