using System.Security.Cryptography;
using System.Text;

namespace LioConecta.Infrastructure.Security;

public static class SecretProtector
{
    public static string Protect(string value, string encryptionKey)
    {
        var plainBytes = Encoding.UTF8.GetBytes(value);
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(encryptionKey));
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var output = new MemoryStream();

        output.Write(aes.IV, 0, aes.IV.Length);

        using (var cryptoStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
        {
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
            cryptoStream.FlushFinalBlock();
        }

        return Convert.ToBase64String(output.ToArray());
    }

    public static string Unprotect(string protectedValue, string encryptionKey)
    {
        var protectedBytes = Convert.FromBase64String(protectedValue);
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(encryptionKey));

        var ivLength = aes.BlockSize / 8;
        var iv = protectedBytes.Take(ivLength).ToArray();
        var cipherBytes = protectedBytes.Skip(ivLength).ToArray();
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var input = new MemoryStream(cipherBytes);
        using var cryptoStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
