using Heimevernet.Models;

namespace Heimevernet.DTOs.YourData;

/// <summary>
/// Downloadable copy of the data stored about a user.
/// Deliberately excludes credentials (password hash, two-factor secret).
/// </summary>
/// <param name="ExportedAt">When the export was made (UTC).</param>
/// <param name="Account">The user's account details.</param>
/// <param name="Needs">Needs the user registered.</param>
/// <param name="Resources">Resources the user registered.</param>
/// <param name="Activity">The user's own audit history, newest first.</param>
public sealed record YourDataExportDto(
    DateTime ExportedAt,
    AccountExportDto Account,
    IReadOnlyList<NeedExportDto> Needs,
    IReadOnlyList<ResourceExportDto> Resources,
    IReadOnlyList<ActivityExportDto> Activity);

/// <summary>
/// Account part of the data export. Has no password hash or 2FA secret.
/// </summary>
/// <param name="Id">ID of the user.</param>
/// <param name="Username">Username.</param>
/// <param name="Email">Email address.</param>
/// <param name="Role">Role.</param>
/// <param name="ActorType">Actor type text.</param>
/// <param name="TwoFactorEnabled">Whether 2FA is on.</param>
/// <param name="CreatedAt">When the account was created (UTC).</param>
public sealed record AccountExportDto(
    int Id,
    string Username,
    string Email,
    UserRole Role,
    string ActorType,
    bool TwoFactorEnabled,
    DateTime CreatedAt);

/// <summary>
/// One need in the data export.
/// </summary>
/// <param name="Id">ID of the need.</param>
/// <param name="Title">Title.</param>
/// <param name="Description">Description, if any.</param>
/// <param name="Category">Category name, if the category still exists.</param>
/// <param name="Region">Region name.</param>
/// <param name="Latitude">Latitude in decimal degrees.</param>
/// <param name="Longitude">Longitude in decimal degrees.</param>
/// <param name="Priority">Priority.</param>
/// <param name="Status">Status.</param>
/// <param name="QuantityNeeded">How many units are needed.</param>
/// <param name="Deadline">Deadline, if any.</param>
/// <param name="ContactPoint">Contact details.</param>
/// <param name="CreatedAt">When the need was registered (UTC).</param>
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

/// <summary>
/// One resource in the data export.
/// </summary>
/// <param name="Id">ID of the resource.</param>
/// <param name="Title">Title.</param>
/// <param name="Description">Description, if any.</param>
/// <param name="Category">Category name, if the category still exists.</param>
/// <param name="Region">Region name.</param>
/// <param name="Latitude">Latitude in decimal degrees.</param>
/// <param name="Longitude">Longitude in decimal degrees.</param>
/// <param name="Status">Status.</param>
/// <param name="QuantityOffered">How many units are offered.</param>
/// <param name="AvailableFrom">When the resource becomes available.</param>
/// <param name="AvailableTo">When it stops being available, if set.</param>
/// <param name="ContactPoint">Contact details.</param>
/// <param name="CreatedAt">When the resource was registered (UTC).</param>
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

/// <summary>
/// One activity (audit) entry in the data export.
/// </summary>
/// <param name="OccurredAt">When it happened (UTC).</param>
/// <param name="Action">What happened.</param>
/// <param name="EntityType">Type name of the affected entity.</param>
/// <param name="EntityId">ID of the affected entity, if any.</param>
/// <param name="Details">Optional extra details.</param>
public sealed record ActivityExportDto(
    DateTime OccurredAt,
    AuditAction Action,
    string EntityType,
    int? EntityId,
    string? Details);