namespace Heimevernet.IntegrationTests.Infrastructure;

/// <summary>
/// Utility helper for parsing antiforgery tokens from HTML forms and executing authenticated form POST requests in integration tests.
/// </summary>
public static class AntiforgeryHelper
{
    /// <summary>
    /// Performs an HTTP GET request to the specified page and extracts the hidden <c>__RequestVerificationToken</c> field value from the HTML response.
    /// </summary>
    /// <param name="client">The configured test <see cref="HttpClient"/>.</param>
    /// <param name="requestUri">The relative URL path rendering the target form.</param>
    /// <returns>The extracted antiforgery token string.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the antiforgery token input field is missing from the returned HTML body.</exception>
    public static async Task<string> GetTokenAsync(HttpClient client, string requestUri)
    {
        // 1. Fetch the page HTML containing the form.
        var response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        // 2. Extract the hidden request verification token using regex.
        var html = await response.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(
            html,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");

        if (!match.Success)
        {
            throw new InvalidOperationException($"Antiforgery token not found on {requestUri}");
        }

        return match.Groups[1].Value;
    }

    /// <summary>
    /// Fetches an antiforgery token from a specified page and submits a form payload via HTTP POST.
    /// </summary>
    /// <param name="client">The test <see cref="HttpClient"/>.</param>
    /// <param name="requestUri">The POST target endpoint.</param>
    /// <param name="tokenPageUri">The page rendering the form containing the antiforgery token.</param>
    /// <param name="formData">Dictionary of form key-value pairs to submit.</param>
    /// <returns>The <see cref="HttpResponseMessage"/> resulting from the POST operation.</returns>
    public static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string requestUri,
        string tokenPageUri,
        Dictionary<string, string> formData)
    {
        // Fetch token and attach it to the form dictionary.
        var token = await GetTokenAsync(client, tokenPageUri);
        formData["__RequestVerificationToken"] = token;

        var content = new FormUrlEncodedContent(formData);
        return await client.PostAsync(requestUri, content);
    }
}