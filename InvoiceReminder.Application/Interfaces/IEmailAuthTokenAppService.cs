using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;

namespace InvoiceReminder.Application.Interfaces;

public interface IEmailAuthTokenAppService : IBaseAppService<EmailAuthToken, EmailAuthTokenViewModel>
{
    Task<Result<EmailAuthTokenViewModel>> GetByUserIdAsync(Guid id, string tokenProvider);
}
