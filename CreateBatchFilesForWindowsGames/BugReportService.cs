using System.Net.Http;
using System.Net.Http.Json;

namespace CreateBatchFilesForWindowsGames;

/// <summary>
/// Service responsible for silently sending bug reports to the BugReport API.
/// This class is designed to be used as a singleton via the App class.
/// </summary>
public class BugReportService
{
    // Use a single, static HttpClient instance for the application's lifetime
    // to prevent socket exhaustion and improve performance.
    private static readonly HttpClient HttpClient = new();

    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationName;

    public BugReportService(string apiUrl, string apiKey, string applicationName)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationName = applicationName;
    }

    /// <summary>
    /// Silently sends a bug report to the API.
    /// </summary>
    /// <param name="message">The error message or bug report.</param>
    /// <returns>A task representing the asynchronous operation, returning true if successful.</returns>
    public async Task<bool> SendBugReportAsync(string message)
    {
        try
        {
            // Create the request payload
            var payload = new
            {
                message,
                applicationName = _applicationName
            };

            // Create a new HttpRequestMessage for each call. This is thread-safe and ensures
            // headers from one request do not interfere with another.
            using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Content = JsonContent.Create(payload);
            request.Headers.Add("X-API-KEY", _apiKey);

            // Send the request using the static HttpClient
            var response = await HttpClient.SendAsync(request);

            // Return true if successful
            return response.IsSuccessStatusCode;
        }
        catch
        {
            // Silently fail if there's an exception
            return false;
        }
    }
}
