using Notes.Infrastructure.Time;

namespace Notes.Infrastructure.UnitTests.Time;

public sealed class SystemDateTimeProviderTests
{
    private readonly SystemDateTimeProvider _sut = new();

    [Fact]
    public void UtcNow_is_expressed_in_utc()
    {
        Assert.Equal(DateTimeKind.Utc, _sut.UtcNow.Kind);
    }

    [Fact]
    public void UtcNow_carries_no_sub_millisecond_component()
    {
        // Values land in DATETIME2(3); sub-millisecond ticks would be rounded by
        // SQL Server and the round-tripped value would no longer match.
        for (var i = 0; i < 50; i++)
        {
            Assert.Equal(0, _sut.UtcNow.Ticks % TimeSpan.TicksPerMillisecond);
        }
    }

    [Fact]
    public void UtcNow_tracks_the_real_clock()
    {
        Assert.InRange(_sut.UtcNow, DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
    }
}
