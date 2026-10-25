using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SmartDocs.Services;

public class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>Client HTTP de l'API SmartDocs : adresse configurable, jeton JWT, erreurs lisibles.</summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public string? Token { get; set; }

    /// <summary>10.0.2.2 = le PC hôte vu depuis l'émulateur Android ; localhost pour Windows.</summary>
    public static string DefaultUrl =>
#if ANDROID
        "http://10.0.2.2:5080";
#else
        "http://localhost:5080";
#endif

    public string BaseUrl
    {
        get => Preferences.Default.Get("api_base_url", DefaultUrl).TrimEnd('/');
        set => Preferences.Default.Set("api_base_url", value.Trim().TrimEnd('/'));
    }

    public async Task<bool> PingAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            using var resp = await _http.GetAsync(BaseUrl + "/health", cts.Token);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<T> GetAsync<T>(string path)
    {
        using var resp = await SendAsync(HttpMethod.Get, path);
        return await ReadAsync<T>(resp);
    }

    public async Task<T> PostAsync<T>(string path, object? body = null, bool auth = true)
    {
        HttpContent? content = body == null
            ? null
            : new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        using var resp = await SendAsync(HttpMethod.Post, path, content, auth);
        return await ReadAsync<T>(resp);
    }

    public async Task<T> PutAsync<T>(string path, object body)
    {
        var content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        using var resp = await SendAsync(HttpMethod.Put, path, content);
        return await ReadAsync<T>(resp);
    }

    public async Task<T> DeleteAsync<T>(string path)
    {
        using var resp = await SendAsync(HttpMethod.Delete, path);
        return await ReadAsync<T>(resp);
    }

    /// <summary>Suppression sans corps de réponse (204).</summary>
    public async Task DeleteAsync(string path)
    {
        using var resp = await SendAsync(HttpMethod.Delete, path);
    }

    public async Task<T> PostMultipartAsync<T>(string path, MultipartFormDataContent content)
    {
        using var resp = await SendAsync(HttpMethod.Post, path, content);
        return await ReadAsync<T>(resp);
    }

    public async Task DownloadAsync(string path, string destination)
    {
        using var resp = await SendAsync(HttpMethod.Get, path);
        await using var source = await resp.Content.ReadAsStreamAsync();
        await using var target = File.Create(destination);
        await source.CopyToAsync(target);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage resp)
        => await resp.Content.ReadFromJsonAsync<T>(Json) ?? throw new ApiException(0, "Réponse vide du serveur.");

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content = null, bool auth = true)
    {
        var request = new HttpRequestMessage(method, BaseUrl + path) { Content = content };
        if (auth && !string.IsNullOrEmpty(Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(0, "Serveur injoignable. Vérifiez que l'API est démarrée et que l'adresse du serveur est correcte.");
        }
        catch (TaskCanceledException)
        {
            throw new ApiException(0, "Le serveur ne répond pas (délai dépassé).");
        }

        if (response.IsSuccessStatusCode) return response;
        var error = await ToExceptionAsync(response);
        response.Dispose();
        throw error;
    }

    private static async Task<ApiException> ToExceptionAsync(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("error", out var e) && e.GetString() is { Length: > 0 } message)
                return new ApiException(status, message);
        }
        catch { /* corps non JSON */ }

        return new ApiException(status, status switch
        {
            401 => "Session expirée ou identifiants invalides.",
            403 => "Action non autorisée pour votre rôle.",
            429 => "Trop de tentatives. Patientez une minute puis réessayez.",
            413 => "Le fichier est trop volumineux.",
            _ => $"Erreur du serveur ({status})."
        });
    }
}
