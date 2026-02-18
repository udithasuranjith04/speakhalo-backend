using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private readonly byte[] _encryptionKey;
    private const int KeySize = 256;
    private const int BlockSize = 128;
    private const int Iterations = 10000;
    private static readonly string DefaultSalt = "SpeakHalo2026Salt";

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _logger = logger;
        var key = configuration["Encryption:Key"] ?? throw new InvalidOperationException("Encryption key not configured");
        var salt = configuration["Encryption:Salt"] ?? DefaultSalt;
        _encryptionKey = DeriveKey(key, salt);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        var iv = aes.IV;
        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var msEncrypt = new MemoryStream();

        // Write IV first
        msEncrypt.Write(iv, 0, iv.Length);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Key = _encryptionKey;

        // Extract IV from the beginning
        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Array.Copy(fullCipher, iv, iv.Length);
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }

    public string Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Generate random salt for each hash
        var salt = GenerateSecureRandomBytes(32);
        using var sha256 = SHA256.Create();

        // Combine salt and input
        var saltedInput = new byte[salt.Length + Encoding.UTF8.GetByteCount(input)];
        Buffer.BlockCopy(salt, 0, saltedInput, 0, salt.Length);
        Buffer.BlockCopy(Encoding.UTF8.GetBytes(input), 0, saltedInput, salt.Length, Encoding.UTF8.GetByteCount(input));

        var hash = sha256.ComputeHash(saltedInput);

        // Store salt with hash: [salt][hash]
        var result = new byte[salt.Length + hash.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(hash, 0, result, salt.Length, hash.Length);

        return Convert.ToBase64String(result);
    }

    public bool VerifyHash(string input, string storedHash)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(storedHash))
            return false;

        try
        {
            var storedHashBytes = Convert.FromBase64String(storedHash);

            // Extract salt (first 32 bytes) and hash (remaining bytes)
            var salt = new byte[32];
            var hash = new byte[storedHashBytes.Length - 32];

            Buffer.BlockCopy(storedHashBytes, 0, salt, 0, 32);
            Buffer.BlockCopy(storedHashBytes, 32, hash, 0, hash.Length);

            // Recompute hash with extracted salt
            using var sha256 = SHA256.Create();
            var saltedInput = new byte[salt.Length + Encoding.UTF8.GetByteCount(input)];
            Buffer.BlockCopy(salt, 0, saltedInput, 0, salt.Length);
            Buffer.BlockCopy(Encoding.UTF8.GetBytes(input), 0, saltedInput, salt.Length, Encoding.UTF8.GetByteCount(input));

            var computedHash = sha256.ComputeHash(saltedInput);

            // Compare hashes using constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify hash");
            return false;
        }
    }

    public string GenerateSecureToken(int length = 32)
    {
        var randomBytes = GenerateSecureRandomBytes(length);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            .Substring(0, length);
    }

    public byte[] GenerateSecureRandomBytes(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }

    private static byte[] DeriveKey(string password, string saltString)
    {
        var salt = Encoding.UTF8.GetBytes(saltString);
        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        return deriveBytes.GetBytes(KeySize / 8);
    }
}
