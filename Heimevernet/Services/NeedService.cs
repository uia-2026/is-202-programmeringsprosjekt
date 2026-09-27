using Heimevernet.Mappers;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Services;

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

    public async Task CreateAsync(
        NeedCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var need = _needMapper.ToEntity(model, userId);

        await _needRepository.AddAsync(need, cancellationToken);
        await _unitOfWork.CommitAsync();
    }

    public async Task<IEnumerable<NeedViewModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var needs = await _needRepository.GetAllAsync(cancellationToken);

        return _needMapper.ToViewModels(needs);
    }

    public async Task<IEnumerable<NeedSummaryViewModel>> GetSummariesAsync(CancellationToken cancellationToken = default)
    {
        var needs = await _needRepository.GetAllAsync(cancellationToken);

        return _needMapper.ToSummaryViewModels(needs);
    }

    public async Task<NeedViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var need = await _needRepository.GetByIdAsync(id, cancellationToken);

        return need == null
            ? null
            : _needMapper.ToViewModel(need);
    }
}