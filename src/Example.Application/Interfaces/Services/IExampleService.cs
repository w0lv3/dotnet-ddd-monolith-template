using Example.Application.Models.Examples;

namespace Example.Application.Interfaces.Services;

public interface IExampleService
{
    Task<ExampleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ExampleDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<ExampleDto> CreateAsync(CreateExampleModel model, CancellationToken cancellationToken);

    Task<ExampleDto> UpdateAsync(UpdateExampleModel model, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
