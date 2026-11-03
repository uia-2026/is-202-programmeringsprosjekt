using Heimevernet.DTOs.Search;
using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    private const int MinQueryLength = 2;
    private const int MaxQueryLength = 100;
    private const int DefaultLimit = 10;
    private const int MaxLimit = 20;

    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

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