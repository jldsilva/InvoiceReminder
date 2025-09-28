using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Abstractions;
using Mapster;

namespace InvoiceReminder.Application.AppServices;

public class BaseAppService<TEntity, TEntityViewModel> : IBaseAppService<TEntity, TEntityViewModel>
    where TEntity : class where TEntityViewModel : class
{
    private readonly IBaseRepository<TEntity> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public BaseAppService(IBaseRepository<TEntity> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public virtual async Task<Result<TEntityViewModel>> AddAsync(TEntityViewModel viewModel, CancellationToken cancellationToken = default)
    {
        if (viewModel is null)
        {
            return Result<TEntityViewModel>.Failure($"Parameter {nameof(viewModel)} was Null.");
        }

        var entity = viewModel.Adapt<TEntity>();

        _ = await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TEntityViewModel>.Success(entity.Adapt<TEntityViewModel>());
    }

    public virtual async Task<Result<int>> BulkInsertAsync(ICollection<TEntityViewModel> viewModels, CancellationToken cancellationToken = default)
    {
        if (viewModels is null || viewModels.Count == 0)
        {
            return Result<int>.Failure($"Parameter {nameof(viewModels)} was Null or Empty.");
        }

        var result = await _repository.BulkInsertAsync(viewModels.Adapt<ICollection<TEntity>>(), cancellationToken);

        return Result<int>.Success(result);
    }

    public virtual Result<IEnumerable<TEntityViewModel>> GetAll()
    {
        var entities = _repository.GetAll();

        return !entities.Any()
            ? Result<IEnumerable<TEntityViewModel>>.Failure($"Empty Result.")
            : Result<IEnumerable<TEntityViewModel>>.Success(entities.Adapt<IEnumerable<TEntityViewModel>>());
    }

    public virtual async Task<Result<TEntityViewModel>> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);

        return entity is null
            ? Result<TEntityViewModel>.Failure($"{typeof(TEntityViewModel).Name} with id {id} not Found.")
            : Result<TEntityViewModel>.Success(entity.Adapt<TEntityViewModel>());
    }

    public virtual async Task<Result<TEntityViewModel>> RemoveAsync(TEntityViewModel viewModel, CancellationToken cancellationToken = default)
    {
        if (viewModel is null)
        {
            return Result<TEntityViewModel>.Failure($"Parameter {nameof(viewModel)} was Null.");
        }

        _repository.Remove(viewModel.Adapt<TEntity>());
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TEntityViewModel>.Success(null);
    }

    public virtual async Task<Result<TEntityViewModel>> UpdateAsync(TEntityViewModel viewModel, CancellationToken cancellationToken = default)
    {
        if (viewModel is null)
        {
            return Result<TEntityViewModel>.Failure($"Parameter {nameof(viewModel)} was Null.");
        }

        _ = _repository.Update(viewModel.Adapt<TEntity>());
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TEntityViewModel>.Success(viewModel);
    }
}
