using InvoiceReminder.Domain.Abstractions;

namespace InvoiceReminder.Application.Interfaces;

public interface IBaseAppService<TEntity, TEntityViewModel> where TEntity : class where TEntityViewModel : class
{
    Task<Result<TEntityViewModel>> AddAsync(TEntityViewModel viewModel, CancellationToken cancellationToken = default);
    Task<Result<int>> BulkInsertAsync(ICollection<TEntityViewModel> viewModels, CancellationToken cancellationToken = default);
    Result<IEnumerable<TEntityViewModel>> GetAll();
    Task<Result<TEntityViewModel>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<TEntityViewModel>> RemoveAsync(TEntityViewModel viewModel, CancellationToken cancellationToken = default);
    Task<Result<TEntityViewModel>> UpdateAsync(TEntityViewModel viewModel, CancellationToken cancellationToken = default);
}
