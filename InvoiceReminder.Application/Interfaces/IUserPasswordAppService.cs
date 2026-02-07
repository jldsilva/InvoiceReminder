using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Application.Interfaces;

public interface IUserPasswordAppService : IBaseAppService<UserPassword, UserPasswordViewModel>
{
    Task<Result<UserPasswordViewModel>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ChangePasswordAsync(UserPasswordViewModel viewModel, CancellationToken cancellationToken = default);
}
