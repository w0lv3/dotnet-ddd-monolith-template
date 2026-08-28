using Example.Application.Interfaces.Repositories;
using Example.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Example.Infrastructure.Persistence.Repositories;

public sealed class ExampleRepository(ApplicationDbContext dbContext) : IExampleRepository
{
    public Task<ExampleEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Examples
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExampleEntity>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Examples
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(ExampleEntity entity, CancellationToken cancellationToken)
    {
        await dbContext.Examples.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ExampleEntity entity, CancellationToken cancellationToken)
    {
        dbContext.Examples.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ExampleEntity entity, CancellationToken cancellationToken)
    {
        dbContext.Examples.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
