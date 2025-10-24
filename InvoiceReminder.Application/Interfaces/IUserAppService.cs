using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Application.Interfaces;

public interface IUserAppService : IBaseAppService<User, UserViewModel>
{
    Task<Result<UserViewModel>> GetByEmailAsync(string value, CancellationToken cancellationToken = default);
}
