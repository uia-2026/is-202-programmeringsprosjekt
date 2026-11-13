using Heimevernet.DTOs.Search;
using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// JSON API for the free-text search box.
/// </summary>
/// <remarks>Base route <c>/api/search</c>. Requires a signed-in user (fallback policy).</remarks>
[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    private const int MinQueryLength = 2;
    private const int MaxQueryLength = 100;
    private const int DefaultLimit = 10;
    private const int MaxLimit = 20;

    private readonly ISearchService _searchService;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="searchService">Runs the search.</param>
    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    /// <summary>
    /// Searches needs and resources by title.
    /// </summary>
    /// <remarks><c>GET /api/search?q={text}&amp;limit={n}</c>. Requires a signed-in user.</remarks>
    /// <param name="q">Search text, 2 to 100 characters after trimming.</param>
    /// <param name="limit">Maximum hits per kind, 1 to 20. Default is 10.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>200 OK with a <see cref="SearchResponseDto"/>, or 400 Bad Request when <paramref name="q"/> or <paramref name="limit"/> is out of range.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        var query = q?.Trim();

        if (string.IsNullOrWhiteSpace(query) ||
            query.Length < MinQueryLength ||
            query.Length > MaxQueryLength)
        {
            return BadRequest("Query must contain between 2 and 100 characters.");
        }

        if (limit < 1 || limit > MaxLimit)
        {
            return BadRequest($"Limit must be between 1 and {MaxLimit}.");
        }

        var userId = User.GetUserId();
        var role = User.GetRole();


        var result = await _searchService.SearchAsync(
            query,
            limit,
            userId,
            role,
            cancellationToken);

        return Ok(result);
    }
}