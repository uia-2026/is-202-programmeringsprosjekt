using Heimevernet.Mappers;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Services;

/// <summary>
/// Application service handling operations related to Needs.
/// Orchestrates repository access, mapping and unit-of-work commits.
/// </summary>
public class NeedService : INeedService
{
    private readonly INeedRepository _needRepository;
    private readonly INeedMapper _needMapper;
    private readonly IUnitOfWork _unitOfWork;

    public NeedService(
        INeedRepository repository,
        INeedMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _needRepository = repository;
        _needMapper = mapper;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates a new need for the specified user.
    /// </summary>
    /// <param name="model">The data used to create the need.</param>
    /// <param name="userId">The id of the user creating the need.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    public async Task CreateAsync(
        NeedCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var need = _needMapper.ToEntity(model, userId);

        await _needRepository.AddAsync(need, cancellationToken);
        await _unitOfWork.CommitAsync();
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
}