using Heimevernet.Models;

namespace Heimevernet.DTOs.YourData;

/// <summary>
/// Downloadable copy of the data stored about a user.
/// Deliberately excludes credentials (password hash, two-factor secret).
/// </summary>
public sealed record YourDataExportDto(
    DateTime ExportedAt,
    AccountExportDto Account,
    IReadOnlyList<NeedExportDto> Needs,
    IReadOnlyList<ResourceExportDto> Resources,
    IReadOnlyList<ActivityExportDto> Activity);

public sealed record AccountExportDto(
    int Id,
    string Username,
    string Email,
    UserRole Role,
    string ActorType,
    bool TwoFactorEnabled,
    DateTime CreatedAt);

public sealed record NeedExportDto(
    int Id,
    string Title,
    string? Description,
    string? Category,
    string Region,
    double Latitude,
    double Longitude,
    NeedPriority Priority,
    NeedStatus Status,
    int QuantityNeeded,
    DateTime? Deadline,
    string ContactPoint,
    DateTime CreatedAt);

public sealed record ResourceExportDto(
    int Id,
    string Title,
    string? Description,
    string? Category,
    string Region,
    double Latitude,
    double Longitude,
    ResourceStatus Status,
    int QuantityOffered,
    DateTime AvailableFrom,
    DateTime? AvailableTo,
    string ContactPoint,
    DateTime CreatedAt);

public sealed record ActivityExportDto(
    DateTime OccurredAt,
    AuditAction Action,
    string EntityType,
    int? EntityId,
    string? Details);
