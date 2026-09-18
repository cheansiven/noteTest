using Notes.Domain.Common;

namespace Notes.Domain.Users;

/// <summary>One place to see every way a user operation can fail.</summary>
public static class UserErrors
{
    public static readonly Error EmailRequired =
        Error.Validation("User.Email.Required", "Email is required.");

    public static readonly Error EmailInvalid =
        Error.Validation("User.Email.Invalid", "Email is not a valid address.");

    public static Error EmailTooLong(int max) =>
        Error.Validation("User.Email.TooLong", $"Email must be {max} characters or fewer.");

    public static readonly Error DisplayNameRequired =
        Error.Validation("User.DisplayName.Required", "Display name is required.");

    public static Error DisplayNameLength(int min, int max) =>
        Error.Validation("User.DisplayName.Length", $"Display name must be between {min} and {max} characters.");

    public static readonly Error PasswordRequired =
        Error.Validation("User.Password.Required", "Password is required.");

    public static Error PasswordLength(int min, int max) =>
        Error.Validation("User.Password.Length", $"Password must be between {min} and {max} characters.");

    public static readonly Error EmailAlreadyRegistered =
        Error.Conflict("User.EmailAlreadyRegistered", "An account with that email already exists.");

    /// <summary>
    /// Deliberately identical for an unknown email and a wrong password, so the
    /// endpoint cannot be used to discover which emails are registered.
    /// </summary>
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("User.InvalidCredentials", "Email or password is incorrect.");

    public static readonly Error NotFound =
        Error.NotFound("User.NotFound", "User not found.");
}
