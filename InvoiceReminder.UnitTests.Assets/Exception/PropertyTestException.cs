using System.Diagnostics.CodeAnalysis;

namespace InvoiceReminder.UnitTests.Assets.Exception;

[ExcludeFromCodeCoverage]
public class PropertyTestException : SystemException
{
    public PropertyTestException()
    {
    }

    public PropertyTestException(string message)
        : base(message)
    {
    }

    public PropertyTestException(string message, SystemException innerException)
        : base(message, innerException)
    {
    }
}
