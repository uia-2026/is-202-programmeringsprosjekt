using Heimevernet.Mappers;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Services;

public class NeedService : INeedService
{
    private readonly INeedRepository _needRepository;
    private readonly NeedMapper _needMapper;

    public NeedService(
        INeedRepository repository,
        NeedMapper mapper)
    {
        _needRepository = repository;
        _needMapper = mapper;
    }

    public async Task CreateAsync(
        NeedCreateViewModel model,
        int userId)
    {
        var need = _needMapper.ToEntity(model, userId);

        await _needRepository.AddAsync(need);
    }

    public async Task<IEnumerable<NeedViewModel>> GetAllAsync()
    {
        var needs = await _needRepository.GetAllAsync();

        return _needMapper.ToViewModels(needs);
    }

    public async Task<NeedViewModel?> GetByIdAsync(int id)
    {
        var need = await _needRepository.GetByIdAsync(id);

        return need == null
            ? null
            : _needMapper.ToViewModel(need);
    }
}