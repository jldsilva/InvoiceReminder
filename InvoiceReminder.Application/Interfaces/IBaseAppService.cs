using InvoiceReminder.Application.Abstractions;

namespace InvoiceReminder.Application.Interfaces;

public interface IBaseAppService<TEntity, TEntityViewModel> where TEntity : class where TEntityViewModel : class
{
    Task<Result<TEntityViewModel>> AddAsync(TEntityViewModel viewModel);
    Task<Result<int>> BulkInsertAsync(ICollection<TEntityViewModel> viewModels);
    Result<IEnumerable<TEntityViewModel>> GetAll();
    Task<Result<TEntityViewModel>> GetByIdAsync(Guid id);
    Task<Result<TEntityViewModel>> RemoveAsync(TEntityViewModel viewModel);
    Task<Result<TEntityViewModel>> UpdateAsync(TEntityViewModel viewModel);
}
