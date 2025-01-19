namespace RedactorApi.Analyzer;

public static class CryptoHelper
{
    public static string EncryptString(string plainText, string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var keyDerivation = new Rfc2898DeriveBytes(secret, salt, 100_000, HashAlgorithmName.SHA256);
        var key = keyDerivation.GetBytes(32); 
        var iv = keyDerivation.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(salt, 0, salt.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            var plaintextBytes = Encoding.UTF8.GetBytes(plainText);
            cs.Write(plaintextBytes, 0, plaintextBytes.Length);
            cs.FlushFinalBlock();
        }

        // The memory stream now contains: [salt][encrypted data]
        var cipherBytes = ms.ToArray();
        Console.WriteLine($"It took {cipherBytes.Length} used in memory stream");
        return Convert.ToBase64String(cipherBytes);
    }

    public static string DecryptString(string cipherTextBase64, string secret)
    {
        var cipherBytes = Convert.FromBase64String(cipherTextBase64);

        // Extract the salt from the cipherBytes : [salt][encrypted data]
        var salt = new byte[16];
        Array.Copy(cipherBytes, 0, salt, 0, salt.Length);
        var encryptedData = new byte[cipherBytes.Length - salt.Length];
        Array.Copy(cipherBytes, salt.Length, encryptedData, 0, encryptedData.Length);

        using var keyDerivation = new Rfc2898DeriveBytes(secret, salt, 100_000, HashAlgorithmName.SHA256);
        var key = keyDerivation.GetBytes(32);
        var iv = keyDerivation.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(encryptedData);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}