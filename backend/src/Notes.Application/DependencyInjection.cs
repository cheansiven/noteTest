using Microsoft.Extensions.DependencyInjection;
using Notes.Application.Notes;
using Notes.Application.Users;

namespace Notes.Application;

/// <summary>
/// Each layer registers its own services, so Program.cs does not need to know the
/// internals of any of them.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INoteService, NoteService>();

        return services;
    }
}
