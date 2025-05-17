using InvoiceReminder.API.AuthenticationSetup;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using NSubstitute;
using Shouldly;

namespace InvoiceReminder.API.UnitTests.AuthenticationSetup;

[TestClass]
public sealed class BearerSecuritySchemeTransformerTests
{
    private readonly OpenApiDocument _document;
    private readonly OpenApiDocumentTransformerContext _context;
    private readonly BearerSecuritySchemeTransformer _transformer;
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

    public BearerSecuritySchemeTransformerTests()
    {
        _context = new OpenApiDocumentTransformerContext
        {
            DocumentName = "TestDocument",
            DescriptionGroups = [],
            ApplicationServices = Substitute.For<IServiceProvider>()
        };

        _authenticationSchemeProvider = Substitute.For<IAuthenticationSchemeProvider>();
        _transformer = new BearerSecuritySchemeTransformer(_authenticationSchemeProvider);
        _document = new OpenApiDocument();
    }

    [TestMethod]
    public async Task TransformAsync_BearerSchemeExists_ShouldAddSecuritySchemeToDocument()
    {
        // Arrange
        _ = _authenticationSchemeProvider.GetAllSchemesAsync().Returns(
        [
            new AuthenticationScheme("Bearer", "Bearer Authentication", typeof(MockAuthenticationHandler))
        ]);

        // Act
        await _transformer.TransformAsync(_document, _context, CancellationToken.None);

        // Assert
        _document.ShouldSatisfyAllConditions(() =>
        {
            _ = _document.ShouldNotBeNull();
            _ = _document.ShouldBeOfType<OpenApiDocument>();
            _ = _document.Components.ShouldNotBeNull();
            _ = _document.Components.SecuritySchemes.ShouldNotBeNull();
            _ = _document.Components.SecuritySchemes.ContainsKey("Bearer");

            _document.Components.SecuritySchemes["Bearer"].Scheme.ShouldBeEquivalentTo("bearer");
        });
    }

    [TestMethod]
    public async Task TransformAsync_NoBearerScheme_ShouldNotModifyDocument()
    {
        // Arrange
        _ = _authenticationSchemeProvider.GetAllSchemesAsync().Returns(
        [
            new AuthenticationScheme("OtherScheme", "Other Authentication", typeof(MockAuthenticationHandler))
        ]);

        // Act
        await _transformer.TransformAsync(_document, _context, CancellationToken.None);

        // Assert
        _document.ShouldSatisfyAllConditions(() => _document.Components?.SecuritySchemes.ShouldBeNull());
    }

    [TestMethod]
    public async Task TransformAsync_NullDocument_ShouldThrowArgumentNullException()
    {
        // Arrange && Act
        var exception = await Should.ThrowAsync<ArgumentNullException>(() =>
            _transformer.TransformAsync(null, _context, CancellationToken.None));

        // Assert
        _ = exception.ShouldNotBeNull();
        exception.ParamName.ShouldBe("document");
    }

    [TestMethod]
    public async Task TransformAsync_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange && Act
        var exception = await Should.ThrowAsync<ArgumentNullException>(() =>
            _transformer.TransformAsync(_document, null, CancellationToken.None));

        // Assert
        _ = exception.ShouldNotBeNull();
        exception.ParamName.ShouldBe("context");
    }
}
