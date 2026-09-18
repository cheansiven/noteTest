using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Notes.Api.Controllers;
using Notes.Api.Infrastructure;
using Notes.Api.Json;
using Notes.Application.Abstractions;
using Notes.Infrastructure.Security;

namespace Notes.Api.Extensions;

/// <summary>
/// Groups the web-host concerns so Program.cs reads as a table of contents rather than
/// a wall of configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    public const string CorsPolicy = "NotesAppCors";

    public static IServiceCollection AddApiServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                // Enums travel as readable strings ("UpdatedAt"), not integers.
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
            })
            .AddMvcOptions(options =>
            {
                // The JSON output formatter does not advertise application/problem+json,
                // so RFC 7807 responses would otherwise negotiate down to application/json.
                foreach (var formatter in options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>())
                {
                    formatter.SupportedMediaTypes.Add("application/problem+json");
                }
            });

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddCors(configuration);
        services.AddJwtAuthentication(configuration);
        services.AddAuthenticationRateLimiting(configuration);
        services.AddSwagger();

        return services;
    }

    private static void AddCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5273"];

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    // Tokens should expire when they say they do, give or take clock drift.
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                options.Events = new JwtBearerEvents
                {
                    // A JWT stays cryptographically valid until it expires, so a token
                    // issued to an account that has since been removed would still pass
                    // signature checks - and then fail deep in the data layer against a
                    // foreign key, surfacing as a 500. Confirm the subject still exists
                    // so a stale session is rejected cleanly as 401 instead.
                    OnTokenValidated = async context =>
                    {
                        if (context.Principal is null || !context.Principal.TryGetUserId(out var userId))
                        {
                            context.Fail("The token does not carry a usable subject.");
                            return;
                        }

                        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();

                        if (!await users.ExistsAsync(userId, context.HttpContext.RequestAborted))
                        {
                            context.Fail("The account this token was issued to no longer exists.");
                        }
                    },
                };
            });

        services.AddAuthorization();
    }

    private static void AddAuthenticationRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Configurable so a test or a load-test environment can raise it without a rebuild.
        var permitLimit = configuration.GetValue<int?>("RateLimiting:Authentication:PermitLimit") ?? 30;
        var windowSeconds = configuration.GetValue<int?>("RateLimiting:Authentication:WindowSeconds") ?? 60;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Partitioned per client address: one noisy caller cannot lock everyone out.
            options.AddPolicy(RateLimitPolicies.Authentication, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0,
                    }));
        });
    }

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Notes API", Version = "v1" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the token returned by /api/auth/login.",
            });

            // Applies the bearer scheme to every operation so the "Authorize" button works.
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(),
            });

            foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "Notes.*.xml"))
            {
                options.IncludeXmlComments(xml);
            }
        });
    }
}
