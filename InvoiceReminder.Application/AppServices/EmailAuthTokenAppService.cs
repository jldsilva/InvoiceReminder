using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;
using Mapster;

namespace InvoiceReminder.Application.AppServices;

public class EmailAuthTokenAppService : BaseAppService<EmailAuthToken, EmailAuthTokenViewModel>, IEmailAuthTokenAppService
{
    private readonly IEmailAuthTokenRepository _repository;

    public EmailAuthTokenAppService(IEmailAuthTokenRepository repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
    {
        _repository = repository;
    }

    public async Task<Result<EmailAuthTokenViewModel>> GetByUserIdAsync(Guid id, string tokenProvider)
    {
        var entity = await _repository.GetByUserIdAsync(id, tokenProvider);

        return entity is null
            ? Result<EmailAuthTokenViewModel>.Failure("EmailAuthToken not Found.")
            : Result<EmailAuthTokenViewModel>.Success(entity.Adapt<EmailAuthTokenViewModel>());
    }
}
