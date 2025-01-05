using System;
using System.IO;
using System.Security.Cryptography;

namespace ExcelDataTool.Core;

public static class DataEncryption
{
    private const int KeySize = 32;
    private const int IvSize = 16;
    
    public static string EncryptData(string data, string key)
    {
        try
        {
            using var deriveBytes = new Rfc2898DeriveBytes(key, new byte[16], 10000, HashAlgorithmName.SHA256);
            byte[] keyBytes = deriveBytes.GetBytes(KeySize);
            byte[] iv = deriveBytes.GetBytes(IvSize);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var msEncrypt = new MemoryStream();
            using var cryptoStream = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using (var swEncrypt = new StreamWriter(cryptoStream))
            {
                swEncrypt.Write(data);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex)
        {
            throw new Exception($"암호화 중 오류가 발생했습니다: {ex.Message}");
        }
    }

    public static string DecryptData(string encryptedData, string key)
    {
        try
        {
            using var deriveBytes = new Rfc2898DeriveBytes(key, new byte[16], 10000, HashAlgorithmName.SHA256);
            byte[] keyBytes = deriveBytes.GetBytes(KeySize);
            byte[] iv = deriveBytes.GetBytes(IvSize);

            byte[] cipherText = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var msDecrypt = new MemoryStream(cipherText);
            using var cryptoStream = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(cryptoStream);
            
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new Exception($"복호화 중 오류가 발생했습니다: {ex.Message}");
        }
    }

    public static bool ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;
            
        return key.Length >= 8;
    }
}