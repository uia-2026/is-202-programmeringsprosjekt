using Heimevernet.DTOs.YourData;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.YourData;

namespace Heimevernet.Services;

/// <summary>
/// Builds the "Your data" overview and the JSON export of the user's own data.
/// </summary>
/// <param name="userRepository">Repository used to load the user and their listings.</param>
/// <param name="auditRepository">Repository used to load the user's own activity.</param>
public class YourDataService(
    IUserRepository userRepository,
    IAuditRepository auditRepository) : IYourDataService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IAuditRepository _auditRepository = auditRepository;

    /// <inheritdoc />
    public async Task<YourDataViewModel?> GetOverviewAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithOwnedDataAsync(userId, cancellationToken);

        if (user is null)
            return null;

        var entries = await _auditRepository.GetByActorAsync(userId, cancellationToken);
        var needRegions = user.Needs.ToDictionary(n => n.Id, n => n.Region);
        var resourceRegions = user.Resources.ToDictionary(r => r.Id, r => r.Region);

        return new YourDataViewModel
        {
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            TwoFactorEnabled = user.TwoFactorEnabled,
            Activity = entries
                .Select(e => new ActivityItemViewModel
                {
                    Action = e.Action,
                    Label = BuildActivityLabel(e, needRegions, resourceRegions),
                    OccurredAt = e.OccurredAt
                })
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task<YourDataExportDto?> ExportAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithOwnedDataAsync(userId, cancellationToken);

        if (user is null)
            return null;

        var entries = await _auditRepository.GetByActorAsync(userId, cancellationToken);

        return new YourDataExportDto(
            DateTime.UtcNow,
            new AccountExportDto(
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.ActorType,
                user.TwoFactorEnabled,
                user.CreatedAt),
            user.Needs
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NeedExportDto(
                    n.Id, n.Title, n.Description, n.Category?.Name, n.Region,
                    n.Latitude, n.Longitude, n.Priority, n.Status, n.QuantityNeeded,
                    n.Deadline, n.ContactPoint, n.CreatedAt))
                .ToList(),
            user.Resources
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ResourceExportDto(
                    r.Id, r.Title, r.Description, r.Category?.Name, r.Region,
                    r.Latitude, r.Longitude, r.Status, r.QuantityOffered,
                    r.AvailableFrom, r.AvailableTo, r.ContactPoint, r.CreatedAt))
                .ToList(),
            entries
                .Select(e => new ActivityExportDto(e.OccurredAt, e.Action, e.EntityType, e.EntityId, e.Details))
                .ToList());
    }

    /// <summary>
    /// Turns an audit entry into a short label, e.g. "Need created — Agder".
    /// Falls back to "#id" when the related need/resource no longer exists.
    /// </summary>
    private static string BuildActivityLabel(
        AuditLog entry,
        IReadOnlyDictionary<int, string> needRegions,
        IReadOnlyDictionary<int, string> resourceRegions)
    {
        var text = DescribeAction(entry.Action);

        if (entry.EntityId is not { } entityId || entry.EntityType == "User")
            return text;

        var regions = entry.EntityType switch
        {
            "Need" => needRegions,
            "Resource" => resourceRegions,
            _ => null
        };

        var subject = regions != null && regions.TryGetValue(entityId, out var region) && !string.IsNullOrWhiteSpace(region)
            ? region
            : $"#{entityId}";

        return $"{text} — {subject}";
    }

    private static string DescribeAction(AuditAction action) => action switch
    {
        AuditAction.LoginSucceeded => "Signed in",
        AuditAction.LoginFailed => "Failed sign-in attempt",
        AuditAction.TwoFactorEnabled => "Two-factor authentication enabled",
        AuditAction.NeedCreated => "Need created",
        AuditAction.NeedEdited => "Need edited",
        AuditAction.NeedDeleted => "Need deleted",
        AuditAction.NeedResolved => "Need resolved",
        AuditAction.ResourceCreated => "Resource created",
        AuditAction.ResourceEdited => "Resource edited",
        AuditAction.ResourceDeleted => "Resource deleted",
        AuditAction.AttachmentUploaded => "Attachment uploaded",
        AuditAction.AttachmentDeleted => "Attachment deleted",
        AuditAction.MatchProposed => "Match proposed",
        AuditAction.MatchAccepted => "Match accepted",
        AuditAction.MatchRejected => "Match rejected",
        AuditAction.MatchResolved => "Match resolved",
        _ => action.ToString()
    };
}