using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace InvoiceReminder.Authentication.Extensions;

public static class StringHashExtension
{
    public static string ToSHA256(this string inputString)
    {
        var bytes = Encoding.UTF8.GetBytes(inputString);
        var hash = SHA256.HashData(bytes);

        return GetStringFromHash(hash);
    }

    public static string ToSHA512(this string inputString)
    {
        var bytes = Encoding.UTF8.GetBytes(inputString);
        var hash = SHA512.HashData(bytes);

        return GetStringFromHash(hash);
    }

    public static string ToMD5(this string inputString)
    {
        var bytes = Encoding.UTF8.GetBytes(inputString);
        var hash = MD5.HashData(bytes);

        return GetStringFromHash(hash);
    }

    private static string GetStringFromHash(byte[] hash)
    {
        var result = new StringBuilder();

        for (var i = 0; i < hash.Length; i++)
        {
            _ = result.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
        }

        return result.ToString();
    }
}
