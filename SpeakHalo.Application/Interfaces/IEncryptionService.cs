namespace SpeakHalo.Application.Interfaces;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string Hash(string input);
    bool VerifyHash(string input, string hash);
    string GenerateSecureToken(int length = 32);
    byte[] GenerateSecureRandomBytes(int length);
}
