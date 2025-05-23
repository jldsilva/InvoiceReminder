using InvoiceReminder.Application.Abstractions;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Application.Interfaces;

public interface IUserAppService : IBaseAppService<User, UserViewModel>
{
    Task<Result<UserViewModel>> GetByEmailAsync(string value);
}
