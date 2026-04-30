using System.Security.Cryptography;
using System.Text;

namespace EasyLogin.Application.Common;

public static class HashHelper
{
    public static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
