using Heimevernet.Mappers;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Services;

public class NeedService : INeedService
{
    private readonly INeedRepository _needRepository;
    private readonly NeedMapper _needMapper;
    private readonly IUnitOfWork _unitOfWork;

    public NeedService(
        INeedRepository repository,
        NeedMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _needRepository = repository;
        _needMapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task CreateAsync(
        NeedCreateViewModel model,
        int userId)
    {
        var need = _needMapper.ToEntity(model, userId);

        await _needRepository.AddAsync(need);
        await _unitOfWork.CommitAsync();
    }

    public async Task<IEnumerable<NeedViewModel>> GetAllAsync()
    {
        var needs = await _needRepository.GetAllAsync();

        return _needMapper.ToViewModels(needs);
    }

    public async Task<IEnumerable<NeedSummaryViewModel>> GetSummariesAsync()
    {
        var needs = await _needRepository.GetAllAsync();

        return _needMapper.ToSummaryViewModels(needs);
    }

    public async Task<NeedViewModel?> GetByIdAsync(int id)
    {
        var need = await _needRepository.GetByIdAsync(id);

        return need == null
            ? null
            : _needMapper.ToViewModel(need);
    }
}