using InvoiceReminder.Application.Abstractions;
using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Entities;
using Mapster;

namespace InvoiceReminder.Application.AppServices;

public class InvoiceAppService : BaseAppService<Invoice, InvoiceViewModel>, IInvoiceAppService
{
    private readonly IInvoiceRepository _repository;

    public InvoiceAppService(IInvoiceRepository repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
    {
        _repository = repository;
    }

    public async Task<Result<InvoiceViewModel>> GetByBarcodeAsync(string value)
    {
        var entity = await _repository.GetByBarCodeAsync(value);

        return entity is null
            ? Result<InvoiceViewModel>.Failure("Invoice not Found.")
            : Result<InvoiceViewModel>.Success(entity.Adapt<InvoiceViewModel>());
    }
}
