using Example.Domain.Entities;

namespace Example.Application.Interfaces.Repositories;

public interface IExampleRepository
{
    Task<ExampleEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ExampleEntity>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(ExampleEntity entity, CancellationToken cancellationToken);

    Task UpdateAsync(ExampleEntity entity, CancellationToken cancellationToken);

    Task DeleteAsync(ExampleEntity entity, CancellationToken cancellationToken);
}
