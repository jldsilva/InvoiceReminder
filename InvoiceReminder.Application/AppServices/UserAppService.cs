using InvoiceReminder.Application.Abstractions;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using Mapster;

namespace InvoiceReminder.Application.AppServices;

public sealed class UserAppService : BaseAppService<User, UserViewModel>, IUserAppService
{
    private readonly IUserRepository _repository;

    public UserAppService(IUserRepository repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
    {
        _repository = repository;
    }

    public async Task<Result<UserViewModel>> GetByEmailAsync(string value)
    {
        var entity = await _repository.GetByEmailAsync(value);

        return entity is null
            ? Result<UserViewModel>.Failure("User not Found.")
            : Result<UserViewModel>.Success(entity.Adapt<UserViewModel>());
    }
}
