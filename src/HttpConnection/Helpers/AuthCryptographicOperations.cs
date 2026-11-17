using System.Security.Cryptography;
using System.Text;

namespace HttpConnection.Helpers;

internal class AuthCryptographicOperations
{
    internal static bool CryptographicEquals(string? left, string right)
    {
        if (left is null)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right));
    }
}
