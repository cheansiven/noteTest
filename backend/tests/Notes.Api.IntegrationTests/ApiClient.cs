using System.Net.Http.Json;
using Notes.Application.Contracts;

namespace Notes.Api.IntegrationTests;

/// <summary>Small wrapper so the tests read as intent rather than HTTP plumbing.</summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http) => _http = http;

    public HttpClient Raw => _http;

    public Task<HttpResponseMessage> RegisterAsync(string email, string password, string displayName) =>
        _http.PostAsJsonAsync("/api/auth/register", new { email, displayName, password });

    public async Task<AuthResponse> RegisterOkAsync(
        string email, string password = "Passw0rd!23", string displayName = "Test User")
    {
        var response = await RegisterAsync(email, password, displayName);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _http.PostAsJsonAsync("/api/auth/login", new { email, password });

    public Task<HttpResponseMessage> GetProfileAsync() => _http.GetAsync("/api/auth/me");

    public Task<HttpResponseMessage> CreateNoteAsync(string title, string? content = null) =>
        _http.PostAsJsonAsync("/api/notes", new { title, content });

    public async Task<NoteDto> CreateNoteOkAsync(string title, string? content = null)
    {
        var response = await CreateNoteAsync(title, content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NoteDto>())!;
    }

    public Task<HttpResponseMessage> GetNoteAsync(Guid id) => _http.GetAsync($"/api/notes/{id}");

    public Task<HttpResponseMessage> UpdateNoteAsync(Guid id, string title, string? content) =>
        _http.PutAsJsonAsync($"/api/notes/{id}", new { title, content });

    public Task<HttpResponseMessage> DeleteNoteAsync(Guid id) => _http.DeleteAsync($"/api/notes/{id}");

    public Task<HttpResponseMessage> ListNotesRawAsync(string queryString = "") =>
        _http.GetAsync($"/api/notes{queryString}");

    public async Task<PagedResult<NoteListItemDto>> ListNotesAsync(string queryString = "")
    {
        var response = await ListNotesRawAsync(queryString);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<NoteListItemDto>>())!;
    }
}
