using System.Text;
using Dapper;
using Notes.Application.Abstractions;
using Notes.Application.Contracts;
using Notes.Domain.Notes;

namespace Notes.Infrastructure.Persistence.Repositories;

public sealed class NoteRepository : INoteRepository
{
    private sealed record NoteRow(Guid Id, Guid UserId, string Title, string? Content, DateTime CreatedAt, DateTime UpdatedAt);

    private const int PreviewLength = 180;

    /// <summary>
    /// Whitelist of sortable columns. ORDER BY cannot be parameterised, so the client's
    /// choice is mapped through this dictionary and never concatenated from raw input.
    /// </summary>
    private static readonly IReadOnlyDictionary<NoteSortField, string> SortColumns =
        new Dictionary<NoteSortField, string>
        {
            [NoteSortField.UpdatedAt] = "UpdatedAt",
            [NoteSortField.CreatedAt] = "CreatedAt",
            [NoteSortField.Title] = "Title",
        };

    private readonly IDbConnectionFactory _connectionFactory;

    public NoteRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<PagedResult<NoteListItemDto>> SearchAsync(
        Guid ownerId, NoteQuery query, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("OwnerId", ownerId);

        // Ownership is part of every predicate: a user can never see another user's rows.
        var where = new StringBuilder("WHERE UserId = @OwnerId");

        if (query.Search is { Length: > 0 } search)
        {
            parameters.Add("Search", $"%{EscapeLikePattern(search)}%");
            where.Append(" AND (Title LIKE @Search ESCAPE '\\' OR Content LIKE @Search ESCAPE '\\')");
        }

        if (query.CreatedFrom is { } createdFrom)
        {
            parameters.Add("CreatedFrom", createdFrom);
            where.Append(" AND CreatedAt >= @CreatedFrom");
        }

        if (query.CreatedTo is { } createdTo)
        {
            parameters.Add("CreatedTo", createdTo);
            where.Append(" AND CreatedAt <= @CreatedTo");
        }

        if (query.HasContent is { } hasContent)
        {
            where.Append(hasContent
                ? " AND Content IS NOT NULL AND LTRIM(RTRIM(Content)) <> ''"
                : " AND (Content IS NULL OR LTRIM(RTRIM(Content)) = '')");
        }

        var sortColumn = SortColumns[query.SortBy];
        var direction = query.SortDirection == SortDirection.Asc ? "ASC" : "DESC";

        parameters.Add("Offset", (query.Page - 1) * query.PageSize);
        parameters.Add("PageSize", query.PageSize);
        parameters.Add("PreviewLength", PreviewLength);

        // Id is the tie-breaker so paging stays stable when timestamps collide.
        var sql = $"""
            SELECT  Id,
                    Title,
                    CASE WHEN Content IS NULL THEN NULL
                         ELSE LEFT(Content, @PreviewLength) END AS Preview,
                    CAST(CASE WHEN Content IS NOT NULL AND LTRIM(RTRIM(Content)) <> ''
                              THEN 1 ELSE 0 END AS BIT)          AS HasContent,
                    CreatedAt,
                    UpdatedAt
            FROM    dbo.Notes
            {where}
            ORDER BY {sortColumn} {direction}, Id {direction}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM dbo.Notes {where};
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            sql, parameters, cancellationToken: cancellationToken));

        var items = (await multi.ReadAsync<NoteListItemDto>()).AsList();
        var total = await multi.ReadSingleAsync<int>();

        return new PagedResult<NoteListItemDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<Note?> GetAsync(Guid ownerId, Guid noteId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<NoteRow>(new CommandDefinition(
            """
            SELECT Id, UserId, Title, Content, CreatedAt, UpdatedAt
            FROM   dbo.Notes
            WHERE  Id = @Id AND UserId = @OwnerId;
            """,
            new { Id = noteId, OwnerId = ownerId },
            cancellationToken: cancellationToken));

        return row is null
            ? null
            : Note.FromPersistence(row.Id, row.UserId, row.Title, row.Content, row.CreatedAt, row.UpdatedAt);
    }

    public async Task AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Notes (Id, UserId, Title, Content, CreatedAt, UpdatedAt)
            VALUES (@Id, @OwnerId, @Title, @Content, @CreatedAt, @UpdatedAt);
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            sql, ToParameters(note), cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(Note note, CancellationToken cancellationToken = default)
    {
        // The owner predicate is repeated here even though the note was loaded scoped:
        // it keeps the write safe on its own terms, independent of how it was fetched.
        const string sql = """
            UPDATE dbo.Notes
            SET    Title = @Title,
                   Content = @Content,
                   UpdatedAt = @UpdatedAt
            WHERE  Id = @Id AND UserId = @OwnerId;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, ToParameters(note), cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid ownerId, Guid noteId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM dbo.Notes WHERE Id = @Id AND UserId = @OwnerId;",
            new { Id = noteId, OwnerId = ownerId },
            cancellationToken: cancellationToken));

        return affected > 0;
    }

    private static object ToParameters(Note note) => new
    {
        note.Id,
        OwnerId = note.OwnerId,
        Title = note.Title.Value,
        Content = note.Content.Value,
        note.CreatedAt,
        note.UpdatedAt,
    };

    /// <summary>Neutralises LIKE wildcards so a search for "50%" is a literal search.</summary>
    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_")
        .Replace("[", "\\[");
}
