using Heimevernet.Mappers.Interfaces;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Moq;

namespace Heimevernet.UnitTests.Services;

public class MatchServiceTests
{
    [Fact]
    public async Task ProposeAsync_Marks_Resource_For_Concurrency_Check()
    {
        var need = new Need { Id = 1, UserId = 42, Status = NeedStatus.New };
        var resource = new Resource { Id = 2, Status = ResourceStatus.Available };
        var needRepository = new Mock<INeedRepository>();
        var resourceRepository = new Mock<IResourceRepository>();
        var matchRepository = new Mock<IMatchRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var candidateMapper = new Mock<IMatchCandidateMapper>();
        var service = new MatchService(
            needRepository.Object,
            resourceRepository.Object,
            matchRepository.Object,
            unitOfWork.Object,
            candidateMapper.Object);

        needRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(need);
        resourceRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);
        matchRepository.Setup(r => r.ExistsAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var (result, _) = await service.ProposeAsync(1, 2, 42);

        Assert.Equal(ProposeMatchResult.Success, result);
        resourceRepository.Verify(r => r.MarkForConcurrencyCheck(resource), Times.Once);
    }
}
