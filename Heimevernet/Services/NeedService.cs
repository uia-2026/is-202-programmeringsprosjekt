using Heimevernet.Infrastructure.Geo;
using Heimevernet.Exceptions;
using Heimevernet.Models;
using Heimevernet.Mappers;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.Services.Rules;
using Heimevernet.ViewModels.Map;
using Heimevernet.ViewModels.Need;
using Heimevernet.DTOs.Needs;
using Heimevernet.Infrastructure.Outbox;
using Heimevernet.Infrastructure.Notifications;

namespace Heimevernet.Services;

/// <summary>
/// Application service handling operations related to Needs.
/// Orchestrates repository access, mapping and unit-of-work commits.
/// </summary>
public class NeedService(
    INeedRepository repository,
    ICategoryRepository categoryRepository,
    INeedMapper needMapper,
    IMapMarkerMapper mapMarkerMapper,
    IUnitOfWork unitOfWork,
    IOutbox outBox,
    IAuditLogger auditLogger) : INeedService
{
    private readonly INeedRepository _needRepository = repository;
    private readonly INeedMapper _needMapper = needMapper;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IMapMarkerMapper _mapMarkerMapper = mapMarkerMapper;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IOutbox _outbox = outBox;
    private readonly IAuditLogger _auditLogger = auditLogger;

    /// <summary>
    /// Creates a new need for the specified user.
    /// </summary>
    /// <param name="model">The data used to create the need.</param>
    /// <param name="userId">The id of the user creating the need.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    public async Task<int> CreateAsync(
        NeedCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var need = _needMapper.ToEntity(model, userId);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _needRepository.AddAsync(need, ct);
            await _unitOfWork.CommitAsync(ct);

            _auditLogger.Record(AuditAction.NeedCreated, "Need", need.Id);

            if (need.Priority == NeedPriority.Critical)
            {
                _outbox.Enqueue(
                OutboxTypes.CriticalNeedRegistered,
                new CriticalNeedRegisteredPayload(need.Id),
                dedupKey: $"critical-need-registered:{need.Id}");
            }

            await _unitOfWork.CommitAsync(ct);
        }, cancellationToken);

        return need.Id;
    }

    /// <summary>
    /// Gets all needs.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A collection of mapped need view models.</returns>
    public async Task<IEnumerable<NeedViewModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var needs = await _needRepository.GetAllAsync(cancellationToken);

        return _needMapper.ToViewModels(needs);
    }

    /// <summary>
    /// Gets summary information for all needs.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A collection of mapped need summary view models.</returns>
    public async Task<IEnumerable<NeedSummaryViewModel>> GetSummariesAsync(CancellationToken cancellationToken = default)
    {
        var needs = await _needRepository.GetAllAsync(cancellationToken);

        return _needMapper.ToSummaryViewModels(needs);
    }

    /// <summary>
    /// Gets needs matching the given filter/sort criteria.
    /// </summary>
    /// <param name="filter">The filter and sort options to apply.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A collection of mapped need view models.</returns>
    public async Task<IEnumerable<NeedViewModel>> GetFilteredAsync(NeedFilterDto filter, CancellationToken cancellationToken = default)
    {
        var needs = await _needRepository.GetFilteredAsync(filter, cancellationToken);

        return _needMapper.ToViewModels(needs);
    }

    public async Task<IReadOnlyList<string>> GetDistinctRegionsAsync(CancellationToken cancellationToken = default) =>
    await _needRepository.GetDistinctRegionsAsync(cancellationToken);

    /// <summary>
    /// Gets a need by id.
    /// </summary>
    /// <param name="id">The id of the need to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The mapped need view model, or <see langword="null"/> if not found.</returns>
    public async Task<NeedViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var need = await _needRepository.GetByIdAsync(id, cancellationToken);

        return need == null
            ? null
            : _needMapper.ToViewModel(need);
    }

    public async Task<IReadOnlyList<MapMarkerViewModel>> GetMapMarkersAsync(
        GeoBox box, int limit, CancellationToken cancellationToken = default)
    {
        var dtos = await _needRepository.GetMapMarkersAsync(box, limit, cancellationToken);
        return _mapMarkerMapper.ToViewModels(dtos);
    }

    public async Task<(OwnedRecordMutationResult Result, string Message)> UpdateAsync(
        int id,
        NeedCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var need = await _needRepository.GetByIdForUpdateAsync(id, cancellationToken);

        if (need is null)
        {
            return (OwnedRecordMutationResult.NotFound, "Need not found.");
        }

        var decision = OwnedRecordPolicy.CanEditNeed(need, model, userId);

        if (!decision.Allowed)
        {
            var result = decision.FailureCode == OwnedRecordFailureCode.NotOwner
                ? OwnedRecordMutationResult.Forbidden
                : OwnedRecordMutationResult.Conflict;

            return (result, decision.Message);
        }

        if (model.CategoryId is not { } categoryId
            || await _categoryRepository.GetByIdAsync(categoryId) is null)
        {
            return (OwnedRecordMutationResult.ValidationError, "Please select a valid category.");
        }

        var changedFields = GetChangedFields(need, model);
        _needMapper.ApplyEditableFields(need, model);
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                _auditLogger.Record(
                    AuditAction.NeedEdited,
                    "Need",
                    need.Id,
                    $"Changed fields: {string.Join(", ", changedFields)}");
                await _unitOfWork.CommitAsync(ct);
            }, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return (OwnedRecordMutationResult.Conflict, "This need changed while you were editing it. Please reload and try again.");
        }

        return (OwnedRecordMutationResult.Success, "Need updated.");
    }

    public async Task<(OwnedRecordMutationResult Result, string Message)> DeleteAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var need = await _needRepository.GetByIdForUpdateAsync(id, cancellationToken);

        if (need is null)
        {
            return (OwnedRecordMutationResult.NotFound, "Need not found.");
        }

        var decision = OwnedRecordPolicy.CanDeleteNeed(need, userId);

        if (!decision.Allowed)
        {
            var result = decision.FailureCode == OwnedRecordFailureCode.NotOwner
                ? OwnedRecordMutationResult.Forbidden
                : OwnedRecordMutationResult.Conflict;

            return (result, decision.Message);
        }

        _needRepository.Remove(need);
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                _auditLogger.Record(AuditAction.NeedDeleted, "Need", need.Id);
                await _unitOfWork.CommitAsync(ct);
            }, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return (OwnedRecordMutationResult.Conflict, "This need changed while you were deleting it. Please reload and try again.");
        }

        return (OwnedRecordMutationResult.Success, "Need deleted.");
    }

    public async Task<(OwnedRecordMutationResult Result, string Message)> ResolveAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var need = await _needRepository.GetByIdForUpdateAsync(id, cancellationToken);

        if (need is null)
        {
            return (OwnedRecordMutationResult.NotFound, "Need not found.");
        }

        if (need.UserId != userId)
        {
            return (OwnedRecordMutationResult.Forbidden, "You do not own this need.");
        }

        if (need.Status == NeedStatus.Resolved)
        {
            return (OwnedRecordMutationResult.Conflict, "Need is already resolved.");
        }

        // Only a need that has been assigned help can be marked as resolved.
        if (need.Status != NeedStatus.Assigned)
        {
            return (OwnedRecordMutationResult.Conflict, "Only assigned needs can be resolved.");
        }

        // Other proposals were already cancelled when the Need became Assigned, so only Assigned matches need resolving.
        var assignedMatches = need.Matches
            .Where(m => m.Status == MatchStatus.Assigned)
            .ToList();

        need.Status = NeedStatus.Resolved;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var match in assignedMatches)
                {
                    match.Status = MatchStatus.Resolved;
                    _auditLogger.Record(AuditAction.MatchResolved, "Match", match.Id);
                }

                _auditLogger.Record(AuditAction.NeedResolved, "Need", need.Id);
                await _unitOfWork.CommitAsync(ct);
            }, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return (OwnedRecordMutationResult.Conflict, "This need changed while it was being resolved. Please try again.");
        }

        return (OwnedRecordMutationResult.Success, "Need resolved.");
    }

    private static IReadOnlyList<string> GetChangedFields(Need need, NeedCreateViewModel model)
    {
        var fields = new List<string>();
        if (need.Title != model.Title.Trim()) fields.Add(nameof(Need.Title));
        if (need.Description != model.Description?.Trim()) fields.Add(nameof(Need.Description));
        if (need.CategoryId != model.CategoryId) fields.Add(nameof(Need.CategoryId));
        if (need.Region != model.Region.Trim()) fields.Add(nameof(Need.Region));
        if (need.Priority != model.Priority) fields.Add(nameof(Need.Priority));
        if (need.Deadline != model.Deadline) fields.Add(nameof(Need.Deadline));
        if (need.ContactPoint != model.ContactPoint.Trim()) fields.Add(nameof(Need.ContactPoint));
        if (need.Latitude != model.Latitude) fields.Add(nameof(Need.Latitude));
        if (need.Longitude != model.Longitude) fields.Add(nameof(Need.Longitude));
        if (need.QuantityNeeded != model.QuantityNeeded) fields.Add(nameof(Need.QuantityNeeded));
        return fields;
    }
}