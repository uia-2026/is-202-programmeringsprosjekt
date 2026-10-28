using Heimevernet.Exceptions;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Match;
using Moq;
using MatchEntity = Heimevernet.Models.Match;

namespace Heimevernet.UnitTests.Services;

public class MatchServiceTests
{
    private readonly Mock<INeedRepository> _needRepositoryMock = new();
    private readonly Mock<IResourceRepository> _resourceRepositoryMock = new();
    private readonly Mock<IMatchRepository> _matchRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMatchCandidateMapper> _matchCandidateMapperMock = new();
    private readonly Mock<IMatchListMapper> _matchListMapperMock = new();

    private MatchService CreateSut() => new(
        _needRepositoryMock.Object,
        _resourceRepositoryMock.Object,
        _matchRepositoryMock.Object,
        _unitOfWorkMock.Object,
        _matchCandidateMapperMock.Object,
        _matchListMapperMock.Object);

    [Theory]
    [InlineData(0)]
    [InlineData(10_001)]
    public async Task ProposeAsync_InvalidQuantity_ReturnsInvalidQuantity(int quantity)
    {
        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, quantity, 3, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.InvalidQuantity, result);
        Assert.Null(matchId);
        _needRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProposeAsync_Success_CreatesMatchWithRequestedQuantity_AndDoesNotAllocate()
    {
        var need = new Need
        {
            Id = 11,
            UserId = 5,
            Status = NeedStatus.New,
            QuantityNeeded = 9
        };

        var resource = new Resource
        {
            Id = 22,
            Status = ResourceStatus.Available,
            QuantityOffered = 15
        };

        MatchEntity? capturedMatch = null;

        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(need.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(need);

        _resourceRepositoryMock
            .Setup(r => r.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync(need.Id, resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForNeedAsync(need.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForResourceAsync(resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _matchRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()))
            .Callback<MatchEntity, CancellationToken>((m, _) =>
            {
                capturedMatch = m;
                m.Id = 99;
            })
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(need.Id, resource.Id, 4, need.UserId, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.Success, result);
        Assert.Equal(99, matchId);

        Assert.NotNull(capturedMatch);
        Assert.Equal(need.Id, capturedMatch!.NeedId);
        Assert.Equal(resource.Id, capturedMatch.ResourceId);
        Assert.Equal(need.UserId, capturedMatch.MatchedByUserId);
        Assert.Equal(4, capturedMatch.Quantity);

        Assert.Equal(9, need.QuantityNeeded);
        Assert.Equal(15, resource.QuantityOffered);
        Assert.Equal(NeedStatus.UnderReview, need.Status);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProposeAsync_QuantityExceedsNeedRemaining_ReturnsInvalidQuantity_AndDoesNotPersist()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 7, Status = NeedStatus.New, QuantityNeeded = 5 });

        _resourceRepositoryMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resource { Id = 2, Status = ResourceStatus.Available, QuantityOffered = 10 });

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForNeedAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForResourceAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 3, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.InvalidQuantity, result);
        Assert.Null(matchId);
        _matchRepositoryMock.Verify(r => r.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProposeAsync_QuantityExceedsResourceRemaining_ReturnsInvalidQuantity_AndDoesNotPersist()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 7, Status = NeedStatus.New, QuantityNeeded = 10 });

        _resourceRepositoryMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resource { Id = 2, Status = ResourceStatus.Available, QuantityOffered = 5 });

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForNeedAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForResourceAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 4, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.InvalidQuantity, result);
        Assert.Null(matchId);
        _matchRepositoryMock.Verify(r => r.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProposeAsync_QuantityExceedsBothRemainingLimits_ReturnsInvalidQuantity_AndDoesNotPersist()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 7, Status = NeedStatus.New, QuantityNeeded = 6 });

        _resourceRepositoryMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resource { Id = 2, Status = ResourceStatus.Available, QuantityOffered = 4 });

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForNeedAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForResourceAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 10, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.InvalidQuantity, result);
        Assert.Null(matchId);
        _matchRepositoryMock.Verify(r => r.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProposeAsync_QuantityWithinBothLimits_CreatesProposal()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 7, Status = NeedStatus.New, QuantityNeeded = 8 });

        _resourceRepositoryMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resource { Id = 2, Status = ResourceStatus.Available, QuantityOffered = 7 });

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForNeedAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForResourceAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _matchRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()))
            .Callback<MatchEntity, CancellationToken>((m, _) => m.Id = 123)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 4, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.Success, result);
        Assert.Equal(123, matchId);
        _matchRepositoryMock.Verify(r => r.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProposeAsync_Need4_Resource3_Quantity100_ReturnsInvalidQuantity_AndDoesNotPersist()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 7, Status = NeedStatus.New, QuantityNeeded = 4 });

        _resourceRepositoryMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resource { Id = 2, Status = ResourceStatus.Available, QuantityOffered = 3 });

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForNeedAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForResourceAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 100, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.InvalidQuantity, result);
        Assert.Null(matchId);
        _matchRepositoryMock.Verify(r => r.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProposeAsync_NeedNotFound_ReturnsNeedNotFound()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Need?)null);

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 1, 3, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.NeedNotFound, result);
        Assert.Null(matchId);
    }

    [Fact]
    public async Task ProposeAsync_UserNotOwner_ReturnsUserIsNotNeedOwner()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 55, Status = NeedStatus.New });

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 1, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.UserIsNotNeedOwner, result);
        Assert.Null(matchId);
    }

    [Fact]
    public async Task ProposeAsync_NeedNotOpen_ReturnsNeedNotOpen()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 7, Status = NeedStatus.Resolved });

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 1, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.NeedNotOpen, result);
        Assert.Null(matchId);
    }

    [Fact]
    public async Task ProposeAsync_ResourceNotFound_ReturnsResourceNotFound()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 7, Status = NeedStatus.New });

        _resourceRepositoryMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource?)null);

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 1, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.ResourceNotFound, result);
        Assert.Null(matchId);
    }

    [Fact]
    public async Task ProposeAsync_ResourceNotAvailable_ReturnsResourceNotAvailable()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 7, Status = NeedStatus.New });

        _resourceRepositoryMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resource { Id = 2, Status = ResourceStatus.Busy });

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 1, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.ResourceNotAvailable, result);
        Assert.Null(matchId);
    }

    [Fact]
    public async Task ProposeAsync_ExistingMatch_ReturnsMatchAlreadyExists()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 7, Status = NeedStatus.New });

        _resourceRepositoryMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resource { Id = 2, Status = ResourceStatus.Available });

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 1, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.MatchAlreadyExists, result);
        Assert.Null(matchId);
    }

    [Fact]
    public async Task ProposeAsync_DuplicateCommit_ReturnsMatchAlreadyExists()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 7, Status = NeedStatus.New });

        _resourceRepositoryMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resource { Id = 2, Status = ResourceStatus.Available });

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateRecordException(new InvalidOperationException()));

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 1, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.MatchAlreadyExists, result);
        Assert.Null(matchId);
    }

    [Fact]
    public async Task ProposeAsync_ConcurrencyCommit_ReturnsConcurrentChange()
    {
        _needRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Need { Id = 1, UserId = 7, Status = NeedStatus.New });

        _resourceRepositoryMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resource { Id = 2, Status = ResourceStatus.Available });

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException(new InvalidOperationException()));

        var sut = CreateSut();

        var (result, matchId) = await sut.ProposeAsync(1, 2, 1, 7, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.ConcurrentChange, result);
        Assert.Null(matchId);
    }

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
            candidateMapper.Object,
            new Mock<IMatchListMapper>().Object);

        needRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(need);
        resourceRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);
        matchRepository.Setup(r => r.ExistsAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        matchRepository.Setup(r => r.GetAcceptedAllocatedQuantityForNeedAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        matchRepository.Setup(r => r.GetAcceptedAllocatedQuantityForResourceAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var (result, _) = await service.ProposeAsync(1, 2, 1, 42, CancellationToken.None);

        Assert.Equal(ProposeMatchResult.Success, result);
        resourceRepository.Verify(r => r.MarkForConcurrencyCheck(resource), Times.Once);
    }

    // ---------- NYTT I STEG 10: Accept, Reject og My Matches ----------

    private static MatchEntity CreateMatch(
        MatchStatus status = MatchStatus.UnderReview,
        int quantity = 3,
        int resourceOwnerId = 7)
    {
        return new MatchEntity
        {
            Id = 1,
            NeedId = 10,
            ResourceId = 20,
            Quantity = quantity,
            Status = status,
            Need = new Need { Id = 10, QuantityNeeded = 5 },
            Resource = new Resource { Id = 20, UserId = resourceOwnerId, QuantityOffered = 5 }
        };
    }

    private void SetupAllocations(int needAllocated, int resourceAllocated)
    {
        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForNeedAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(needAllocated);

        _matchRepositoryMock
            .Setup(r => r.GetAcceptedAllocatedQuantityForResourceAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resourceAllocated);
    }

    [Fact]
    public async Task AcceptAsync_MatchNotFound_ReturnsMatchNotFound()
    {
        _matchRepositoryMock
            .Setup(r => r.GetByIdWithNeedAndResourceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchEntity?)null);

        var sut = CreateSut();

        var result = await sut.AcceptAsync(1, 7, CancellationToken.None);

        Assert.Equal(MatchDecisionResult.MatchNotFound, result);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptAsync_UserNotResourceOwner_ReturnsUserIsNotResourceOwner_AndDoesNotPersist()
    {
        var match = CreateMatch(resourceOwnerId: 99);

        _matchRepositoryMock
            .Setup(r => r.GetByIdWithNeedAndResourceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var sut = CreateSut();

        var result = await sut.AcceptAsync(1, 7, CancellationToken.None);

        Assert.Equal(MatchDecisionResult.UserIsNotResourceOwner, result);
        Assert.Equal(MatchStatus.UnderReview, match.Status);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptAsync_MatchAlreadyAssigned_ReturnsMatchNotUnderReview()
    {
        var match = CreateMatch(status: MatchStatus.Assigned);

        _matchRepositoryMock
            .Setup(r => r.GetByIdWithNeedAndResourceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var sut = CreateSut();

        var result = await sut.AcceptAsync(1, 7, CancellationToken.None);

        Assert.Equal(MatchDecisionResult.MatchNotUnderReview, result);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptAsync_NotEnoughQuantityLeft_ReturnsInsufficientQuantity_AndDoesNotPersist()
    {
        // Need wants 5, 3 are already accepted, so only 2 are left. The match asks for 3.
        var match = CreateMatch(quantity: 3);

        _matchRepositoryMock
            .Setup(r => r.GetByIdWithNeedAndResourceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        SetupAllocations(needAllocated: 3, resourceAllocated: 0);

        var sut = CreateSut();

        var result = await sut.AcceptAsync(1, 7, CancellationToken.None);

        Assert.Equal(MatchDecisionResult.InsufficientQuantity, result);
        Assert.Equal(MatchStatus.UnderReview, match.Status);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptAsync_Success_SetsAssigned_MarksConcurrency_AndCommits()
    {
        var match = CreateMatch(quantity: 3);

        _matchRepositoryMock
            .Setup(r => r.GetByIdWithNeedAndResourceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        SetupAllocations(needAllocated: 0, resourceAllocated: 0);

        var sut = CreateSut();

        var result = await sut.AcceptAsync(1, 7, CancellationToken.None);

        Assert.Equal(MatchDecisionResult.Success, result);
        Assert.Equal(MatchStatus.Assigned, match.Status);
        _needRepositoryMock.Verify(r => r.MarkForConcurrencyCheck(match.Need), Times.Once);
        _resourceRepositoryMock.Verify(r => r.MarkForConcurrencyCheck(match.Resource), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptAsync_ConcurrencyCommit_ReturnsConcurrentChange()
    {
        var match = CreateMatch();

        _matchRepositoryMock
            .Setup(r => r.GetByIdWithNeedAndResourceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        SetupAllocations(needAllocated: 0, resourceAllocated: 0);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException(new InvalidOperationException()));

        var sut = CreateSut();

        var result = await sut.AcceptAsync(1, 7, CancellationToken.None);

        Assert.Equal(MatchDecisionResult.ConcurrentChange, result);
    }

    [Fact]
    public async Task RejectAsync_UserNotResourceOwner_ReturnsUserIsNotResourceOwner_AndDoesNotRemove()
    {
        var match = CreateMatch(resourceOwnerId: 99);

        _matchRepositoryMock
            .Setup(r => r.GetByIdWithNeedAndResourceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var sut = CreateSut();

        var result = await sut.RejectAsync(1, 7, CancellationToken.None);

        Assert.Equal(MatchDecisionResult.UserIsNotResourceOwner, result);
        _matchRepositoryMock.Verify(r => r.Remove(It.IsAny<MatchEntity>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectAsync_MatchAlreadyAssigned_ReturnsMatchNotUnderReview()
    {
        var match = CreateMatch(status: MatchStatus.Assigned);

        _matchRepositoryMock
            .Setup(r => r.GetByIdWithNeedAndResourceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var sut = CreateSut();

        var result = await sut.RejectAsync(1, 7, CancellationToken.None);

        Assert.Equal(MatchDecisionResult.MatchNotUnderReview, result);
        _matchRepositoryMock.Verify(r => r.Remove(It.IsAny<MatchEntity>()), Times.Never);
    }

    [Fact]
    public async Task RejectAsync_Success_RemovesMatch_AndCommits()
    {
        var match = CreateMatch();

        _matchRepositoryMock
            .Setup(r => r.GetByIdWithNeedAndResourceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var sut = CreateSut();

        var result = await sut.RejectAsync(1, 7, CancellationToken.None);

        Assert.Equal(MatchDecisionResult.Success, result);
        _matchRepositoryMock.Verify(r => r.Remove(match), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyMatchesAsync_ResourceProvider_UsesResourceOwnerQuery()
    {
        var match = CreateMatch();

        _matchRepositoryMock
            .Setup(r => r.GetForResourceOwnerAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MatchEntity> { match });

        _matchListMapperMock
            .Setup(m => m.ToListItem(match, true))
            .Returns(new MatchListItemViewModel { MatchId = match.Id, CanDecide = true });

        var sut = CreateSut();

        var model = await sut.GetMyMatchesAsync(7, true, CancellationToken.None);

        Assert.True(model.IsResourceProvider);
        Assert.Single(model.Matches);
        Assert.True(model.Matches[0].CanDecide);
        _matchRepositoryMock.Verify(r => r.GetForNeedOwnerAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMyMatchesAsync_PublicActor_UsesNeedOwnerQuery()
    {
        _matchRepositoryMock
            .Setup(r => r.GetForNeedOwnerAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MatchEntity>());

        var sut = CreateSut();

        var model = await sut.GetMyMatchesAsync(7, false, CancellationToken.None);

        Assert.False(model.IsResourceProvider);
        Assert.Empty(model.Matches);
        _matchRepositoryMock.Verify(r => r.GetForResourceOwnerAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}