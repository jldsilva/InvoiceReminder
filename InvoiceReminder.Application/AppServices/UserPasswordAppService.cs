using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Authentication.Extensions;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Services.Configuration;
using Mapster;

namespace InvoiceReminder.Application.AppServices;

public class UserPasswordAppService : BaseAppService<UserPassword, UserPasswordViewModel>, IUserPasswordAppService
{
    private readonly int _parallelismFactor;
    private readonly IUserPasswordRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UserPasswordAppService(
        IConfigurationService configuration,
        IUserPasswordRepository repository,
        IUnitOfWork unitOfWork) : base(repository, unitOfWork)
    {
        _parallelismFactor = configuration.GetValue<int>("Security:ParallelismFactor");
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<UserPasswordViewModel>> AddAsync(
        UserPasswordViewModel viewModel,
        CancellationToken cancellationToken = default)
    {
        if (viewModel is null)
        {
            return Result<UserPasswordViewModel>.Failure("The provided obejct data was Null.");
        }

        if (string.IsNullOrWhiteSpace(viewModel.PasswordHash))
        {
            return Result<UserPasswordViewModel>.Failure("Password is required.");
        }

        (var pHash, var pSalt) = viewModel.PasswordHash.HashPassword(_parallelismFactor);

        viewModel.PasswordHash = pHash;
        viewModel.PasswordSalt = pSalt;

        var entity = viewModel.Adapt<UserPassword>();

        _ = await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserPasswordViewModel>.Success(entity.Adapt<UserPasswordViewModel>());
    }

    public override async Task<Result<int>> BulkInsertAsync(
        ICollection<UserPasswordViewModel> viewModelCollection,
        CancellationToken cancellationToken = default)
    {
        if (viewModelCollection is null or { Count: 0 })
        {
            return Result<int>.Failure("The provided object data was Null or Empty.");
        }

        foreach (var viewModel in viewModelCollection)
        {
            if (string.IsNullOrWhiteSpace(viewModel.PasswordHash))
            {
                return Result<int>.Failure("Password is required.");
            }

            (var pHash, var pSalt) = viewModel.PasswordHash.HashPassword(_parallelismFactor);

            viewModel.PasswordHash = pHash;
            viewModel.PasswordSalt = pSalt;
        }

        var result = await _repository
            .BulkInsertAsync(viewModelCollection.Adapt<ICollection<UserPassword>>(), cancellationToken);

        return Result<int>.Success(result);
    }

    public async Task<Result<bool>> ChangePasswordAsync(
        UserPasswordViewModel viewModel,
        CancellationToken cancellationToken = default)
    {
        if (viewModel is null)
        {
            return Result<bool>.Failure("The provided object data was Null.");
        }

        if (string.IsNullOrWhiteSpace(viewModel.PasswordHash))
        {
            return Result<bool>.Failure("Password is required.");
        }

        (var pHash, var pSalt) = viewModel.PasswordHash.HashPassword(_parallelismFactor);
        viewModel.PasswordHash = pHash;
        viewModel.PasswordSalt = pSalt;

        var result = await _repository.ChangePasswordAsync(viewModel.Adapt<UserPassword>(), cancellationToken);

        return result
            ? Result<bool>.Success(result)
            : Result<bool>.Failure("Failed to change the password.");
    }

    public async Task<Result<UserPasswordViewModel>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByUserIdAsync(userId, cancellationToken);

        return entity is null
            ? Result<UserPasswordViewModel>.Failure("No user password found for the specified user ID.")
            : Result<UserPasswordViewModel>.Success(entity.Adapt<UserPasswordViewModel>());
    }

    public override async Task<Result<UserPasswordViewModel>> UpdateAsync(
        UserPasswordViewModel viewModel,
        CancellationToken cancellationToken = default)
    {
        if (viewModel is null)
        {
            return Result<UserPasswordViewModel>.Failure("The provided object data was Null.");
        }

        (var pHash, var pSalt) = viewModel.PasswordHash.HashPassword(_parallelismFactor);

        viewModel.PasswordHash = pHash;
        viewModel.PasswordSalt = pSalt;

        _ = _repository.Update(viewModel.Adapt<UserPassword>());
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserPasswordViewModel>.Success(viewModel);
    }
}
