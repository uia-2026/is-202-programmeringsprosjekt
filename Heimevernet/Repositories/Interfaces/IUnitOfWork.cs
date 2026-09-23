namespace Heimevernet.Repositories.Interfaces;

public interface IUnitOfWork
{
    Task CommitAsync();
}