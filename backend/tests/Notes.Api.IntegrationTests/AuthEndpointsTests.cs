using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Notes.Application.Contracts;

namespace Notes.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthEndpointsTests : IAsyncLifetime
{
    private readonly NotesApiFactory _factory;
    private readonly ApiClient _client;

    public AuthEndpointsTests(NotesApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    public Task InitializeAsync() => _factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Register_returns_201_with_a_token_and_the_new_profile()
    {
        var email = UniqueEmail();

        var response = await _client.RegisterAsync(email, "Passw0rd!23", "Test User");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.Equal(email, auth.User.Email);
        Assert.True(auth.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_is_case_insensitive_about_the_email()
    {
        var email = UniqueEmail();
        await _client.RegisterOkAsync(email);

        var duplicate = await _client.RegisterAsync(email.ToUpperInvariant(), "Passw0rd!23", "Someone Else");

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", "Passw0rd!23", "Test User")]
    [InlineData("valid@example.com", "short", "Test User")]
    [InlineData("valid@example.com", "Passw0rd!23", "A")]
    public async Task Register_rejects_invalid_input_with_an_rfc7807_body(string email, string password, string name)
    {
        var response = await _client.RegisterAsync(email, password, name);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(400, problem!.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
    }

    [Fact]
    public async Task Errors_are_served_as_problem_json_when_the_client_asks_for_it()
    {
        // With no Accept header MVC serves the same body as application/json - the
        // framework's own error responses behave identically.
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new { email = "bad", displayName = "Test User", password = "Passw0rd!23" }),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/problem+json"));

        var response = await _client.Raw.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_returns_a_working_token()
    {
        var email = UniqueEmail();
        await _client.RegisterOkAsync(email);

        var response = await _client.LoginAsync(email, "Passw0rd!23");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        var authenticated = _factory.CreateApiClient(auth!.Token);
        Assert.Equal(HttpStatusCode.OK, (await authenticated.GetProfileAsync()).StatusCode);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_returns_401()
    {
        var email = UniqueEmail();
        await _client.RegisterOkAsync(email);

        var response = await _client.LoginAsync(email, "WrongPassword1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_gives_the_same_answer_for_unknown_and_wrong_password()
    {
        var email = UniqueEmail();
        await _client.RegisterOkAsync(email);

        var wrongPassword = await _client.LoginAsync(email, "WrongPassword1");
        var unknownUser = await _client.LoginAsync(UniqueEmail(), "Passw0rd!23");

        // Identical status and body, so the endpoint cannot enumerate accounts.
        Assert.Equal(wrongPassword.StatusCode, unknownUser.StatusCode);
        Assert.Equal(
            await wrongPassword.Content.ReadAsStringAsync(),
            await unknownUser.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Me_without_a_token_returns_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetProfileAsync()).StatusCode);
    }

    [Fact]
    public async Task A_token_for_a_deleted_account_is_rejected_as_401_not_500()
    {
        // Regression: the token stays signature-valid after the account is gone. Without
        // an existence check the request reached the data layer and blew up against
        // FK_Notes_Users, surfacing to the user as "An unexpected error occurred".
        var auth = await _client.RegisterOkAsync(UniqueEmail());
        var authenticated = _factory.CreateApiClient(auth.Token);

        await _factory.ResetAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await authenticated.CreateNoteAsync("Orphan")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await authenticated.ListNotesRawAsync()).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await authenticated.GetProfileAsync()).StatusCode);
    }

    [Fact]
    public async Task A_tampered_token_is_rejected()
    {
        var auth = await _client.RegisterOkAsync(UniqueEmail());
        // Flip the last character of the signature.
        var tampered = auth.Token[..^1] + (auth.Token[^1] == 'a' ? 'b' : 'a');

        var response = await _factory.CreateApiClient(tampered).GetProfileAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
