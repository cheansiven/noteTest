using System.Data;
using Dapper;

namespace Notes.Infrastructure.Persistence;

internal static class DapperConfiguration
{
    private static bool _configured;

    /// <summary>
    /// Dapper maps <see cref="DateTime"/> to <c>DbType.DateTime</c> by default, which SQL
    /// Server sends as the legacy <c>datetime</c> type. That type has ~3.33 ms resolution
    /// and silently rounds to 1/300-second increments, so a value written as 10:00:00.914
    /// would come back as 10:00:00.913. Our columns are DATETIME2, so map to DateTime2 and
    /// the value stored is exactly the value supplied.
    /// </summary>
    public static void Configure()
    {
        if (_configured)
        {
            return;
        }

        SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2);
        SqlMapper.AddTypeMap(typeof(DateTime?), DbType.DateTime2);

        _configured = true;
    }
}
