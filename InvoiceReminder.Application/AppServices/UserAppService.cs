using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Authentication.Extensions;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;
using Mapster;

namespace InvoiceReminder.Application.AppServices;

public sealed class UserAppService : BaseAppService<User, UserViewModel>, IUserAppService
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UserAppService(IUserRepository repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<UserViewModel>> AddAsync(
        UserViewModel viewModel,
        CancellationToken cancellationToken = default)
    {
        if (viewModel is null)
        {
            return Result<UserViewModel>.Failure($"Parameter {nameof(viewModel)} was Null.");
        }

        (var pHash, var pSalt) = viewModel.UserPassword.PasswordHash.HashPassword();

        viewModel.UserPassword.UserId = viewModel.Id;
        viewModel.UserPassword.PasswordHash = pHash;
        viewModel.UserPassword.PasswordSalt = pSalt;

        var entity = viewModel.Adapt<User>();

        _ = await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserViewModel>.Success(entity.Adapt<UserViewModel>());
    }

    public async Task<Result<UserViewModel>> GetByEmailAsync(
        string value,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByEmailAsync(value, cancellationToken);

        return entity is null
            ? Result<UserViewModel>.Failure("User not Found.")
            : Result<UserViewModel>.Success(entity.Adapt<UserViewModel>());
    }

    public async Task<Result<UserViewModel>> ValidateUserPasswordAsync(
        string email, string password,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByEmailAsync(email, cancellationToken);
        var isValid = entity is not null &&
            password.VerifyPassword(entity.UserPassword.PasswordHash, entity.UserPassword.PasswordSalt);

        return !isValid
            ? Result<UserViewModel>.Failure("User not Found.")
            : Result<UserViewModel>.Success(entity.Adapt<UserViewModel>());
    }
}
