using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;
using Mapster;

namespace InvoiceReminder.Application.AppServices;

public class ScanEmailDefinitionAppService : BaseAppService<ScanEmailDefinition, ScanEmailDefinitionViewModel>,
    IScanEmailDefinitionAppService
{
    private readonly IScanEmailDefinitionRepository _repository;

    public ScanEmailDefinitionAppService(IScanEmailDefinitionRepository repository, IUnitOfWork unitOfWork)
        : base(repository, unitOfWork)
    {
        _repository = repository;
    }

    public async Task<Result<ScanEmailDefinitionViewModel>> GetBySenderBeneficiaryAsync(string value, Guid id)
    {
        var entity = await _repository.GetBySenderBeneficiaryAsync(value, id);

        return entity is null
            ? Result<ScanEmailDefinitionViewModel>.Failure("ScanEmailDefinition not Found.")
            : Result<ScanEmailDefinitionViewModel>.Success(entity.Adapt<ScanEmailDefinitionViewModel>());
    }

    public async Task<Result<ScanEmailDefinitionViewModel>> GetBySenderEmailAddressAsync(string value, Guid id)
    {
        var entity = await _repository.GetBySenderEmailAddressAsync(value, id);

        return entity is null
            ? Result<ScanEmailDefinitionViewModel>.Failure("ScanEmailDefinition not Found.")
            : Result<ScanEmailDefinitionViewModel>.Success(entity.Adapt<ScanEmailDefinitionViewModel>());
    }

    public async Task<Result<IEnumerable<ScanEmailDefinitionViewModel>>> GetByUserIdAsync(Guid userId)
    {
        var entities = await _repository.GetByUserIdAsync(userId);

        return !entities.Any()
            ? Result<IEnumerable<ScanEmailDefinitionViewModel>>.Failure("Empty Result.")
            : Result<IEnumerable<ScanEmailDefinitionViewModel>>.Success(entities.Adapt<IEnumerable<ScanEmailDefinitionViewModel>>());
    }
}
