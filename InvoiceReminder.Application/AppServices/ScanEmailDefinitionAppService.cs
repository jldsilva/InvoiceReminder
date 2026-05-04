using InvoiceReminder.Application.Interfaces;
using InvoiceReminder.Application.ViewModels;
using InvoiceReminder.Authentication.Extensions;
using InvoiceReminder.Data.Interfaces;
using InvoiceReminder.Domain.Abstractions;
using InvoiceReminder.Domain.Entities;
using InvoiceReminder.Domain.Services.Configuration;
using Mapster;

namespace InvoiceReminder.Application.AppServices;

public class ScanEmailDefinitionAppService : BaseAppService<ScanEmailDefinition, ScanEmailDefinitionViewModel>,
    IScanEmailDefinitionAppService
{
    private readonly string _thumbPrint;
    private readonly IScanEmailDefinitionRepository _repository;

    public ScanEmailDefinitionAppService(
        IConfigurationService configuration,
        IScanEmailDefinitionRepository repository,
        IUnitOfWork unitOfWork) : base(repository, unitOfWork)
    {
        _repository = repository;
        _thumbPrint = configuration.GetAppSetting("Security:CertificateThumbprint");
    }

    public override async Task<Result<ScanEmailDefinitionViewModel>> AddAsync(
        ScanEmailDefinitionViewModel viewModel,
        CancellationToken cancellationToken = default)
    {
        if (viewModel is null)
        {
            return Result<ScanEmailDefinitionViewModel>.Failure($"Parameter {nameof(viewModel)} was Null.");
        }

        if (!string.IsNullOrWhiteSpace(viewModel.FilePassword))
        {
            viewModel.FilePassword = viewModel.FilePassword.X509_Encrypt(_thumbPrint);
        }

        return await base.AddAsync(viewModel, cancellationToken);
    }

    public override async Task<Result<ScanEmailDefinitionViewModel>> UpdateAsync(
        ScanEmailDefinitionViewModel viewModel,
        CancellationToken cancellationToken = default)
    {
        if (viewModel is null)
        {
            return Result<ScanEmailDefinitionViewModel>.Failure($"Parameter {nameof(viewModel)} was Null.");
        }

        if (!string.IsNullOrWhiteSpace(viewModel.FilePassword))
        {
            viewModel.FilePassword = viewModel.FilePassword.X509_Encrypt(_thumbPrint);
        }

        return await base.UpdateAsync(viewModel, cancellationToken);
    }

    public async Task<Result<ScanEmailDefinitionViewModel>> GetBySenderBeneficiaryAsync(
        string value,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetBySenderBeneficiaryAsync(value, id, cancellationToken);

        return entity is null
            ? Result<ScanEmailDefinitionViewModel>.Failure("ScanEmailDefinition not Found.")
            : Result<ScanEmailDefinitionViewModel>.Success(entity.Adapt<ScanEmailDefinitionViewModel>());
    }

    public async Task<Result<ScanEmailDefinitionViewModel>> GetBySenderEmailAddressAsync(
        string value,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetBySenderEmailAddressAsync(value, id, cancellationToken);

        return entity is null
            ? Result<ScanEmailDefinitionViewModel>.Failure("ScanEmailDefinition not Found.")
            : Result<ScanEmailDefinitionViewModel>.Success(entity.Adapt<ScanEmailDefinitionViewModel>());
    }

    public async Task<Result<IEnumerable<ScanEmailDefinitionViewModel>>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetByUserIdAsync(userId, cancellationToken);

        return !entities.Any()
            ? Result<IEnumerable<ScanEmailDefinitionViewModel>>.Failure("Empty Result.")
            : Result<IEnumerable<ScanEmailDefinitionViewModel>>.Success(entities.Adapt<IEnumerable<ScanEmailDefinitionViewModel>>());
    }
}
