using Notes.Domain.Common;

namespace Notes.Domain.Notes;

/// <summary>
/// A note owned by exactly one user.
/// <para>
/// The timestamp rules live here rather than in a service or a SQL statement:
/// <see cref="CreatedAt"/> is fixed at creation and <see cref="UpdatedAt"/> moves
/// on every edit, so there is no way to persist a note that violates them.
/// </para>
/// </summary>
public sealed class Note : Entity<Guid>
{
    private Note(Guid id, Guid ownerId, NoteTitle title, NoteContent content, DateTime createdAt, DateTime updatedAt)
        : base(id)
    {
        OwnerId = ownerId;
        Title = title;
        Content = content;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid OwnerId { get; }

    public NoteTitle Title { get; private set; }

    public NoteContent Content { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>True when the note has never been edited since it was written.</summary>
    public bool IsUnedited => UpdatedAt == CreatedAt;

    public static Note Create(Guid ownerId, NoteTitle title, NoteContent content, DateTime utcNow)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("A note must have an owner.", nameof(ownerId));
        }

        // A new note is "updated" at the moment it is created.
        return new Note(Guid.NewGuid(), ownerId, title, content, utcNow, utcNow);
    }

    /// <summary>
    /// Applies an edit. Returns false when nothing actually changed, which lets the
    /// caller skip a pointless write and, more importantly, avoids bumping
    /// <see cref="UpdatedAt"/> for a save that changed nothing.
    /// </summary>
    public bool Edit(NoteTitle title, NoteContent content, DateTime utcNow)
    {
        if (Title == title && Content == content)
        {
            return false;
        }

        Title = title;
        Content = content;
        UpdatedAt = utcNow;
        return true;
    }

    /// <summary>Ownership check expressed once, in the model, rather than at each call site.</summary>
    public bool IsOwnedBy(Guid userId) => OwnerId == userId;

    public static Note FromPersistence(
        Guid id, Guid ownerId, string title, string? content, DateTime createdAt, DateTime updatedAt) =>
        new(id,
            ownerId,
            NoteTitle.FromPersistence(title),
            NoteContent.FromPersistence(content),
            createdAt,
            updatedAt);
}
